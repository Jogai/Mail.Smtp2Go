using System.Net;
using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.RateLimiting;
using Polly.Timeout;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.DependencyInjection;

/// <summary>
/// Builds the <c>HttpClient</c> resilience pipeline from <see cref="ResilienceOptions"/>: rate limiter, total timeout, retry, circuit breaker, attempt timeout.
/// Every strategy reads the <see cref="Endpoint"/> descriptor the core transport attaches to the request through <see cref="RequestOptionKeys"/>.
/// </summary>
internal static class Smtp2GoResilienceBuilder
{
    /// <summary>The resilience handler name; the pipeline key is <c>{httpClientName}-{PipelineName}</c>.</summary>
    public const string PipelineName = "Scott.Mail.Smtp2Go";

    private const string UnknownEndpoint = "(no endpoint descriptor)";

    /// <summary>Entry point for <c>AddResilienceHandler</c>: resolves the named options, logger and <see cref="TimeProvider"/> from the container and rebuilds on options reload.</summary>
    public static void Configure(ResiliencePipelineBuilder<HttpResponseMessage> builder, ResilienceHandlerContext context, string name)
    {
        Argument.ThrowIfNull(builder);
        Argument.ThrowIfNull(context);
        Argument.ThrowIfNull(name);

        context.EnableReloads<Smtp2GoOptions>(name);
        Smtp2GoOptions options = context.GetOptions<Smtp2GoOptions>(name);
        IServiceProvider services = context.ServiceProvider;
        ILogger logger = services.GetService<ILoggerFactory>()?.CreateLogger(ResilienceLog.CategoryName) ?? NullLogger.Instance;
        builder.TimeProvider = services.GetService<TimeProvider>() ?? TimeProvider.System;

        Configure(builder, options.Resilience, logger, context.OnPipelineDisposed);
    }

    /// <summary>Adds the strategies to <paramref name="builder"/>. <paramref name="onDisposed"/> receives the callback that releases the rate limiters.</summary>
    public static void Configure(ResiliencePipelineBuilder<HttpResponseMessage> builder, ResilienceOptions options, ILogger logger, Action<Action>? onDisposed)
    {
        Argument.ThrowIfNull(builder);
        Argument.ThrowIfNull(options);
        Argument.ThrowIfNull(logger);

        // 1. Rate limiter: the documented window of the endpoint's RateLimitClass, then the global concurrency cap (RateLimiting.Enabled, GlobalConcurrency, GlobalQueueLimit, Overrides).
        if (options.RateLimiting.Enabled)
        {
            EndpointRateLimiters limiters = new(options.RateLimiting);
            onDisposed?.Invoke(limiters.Dispose);
            builder.AddRateLimiter(new RateLimiterStrategyOptions
            {
                Name = "Smtp2Go.RateLimiter",
                RateLimiter = args => limiters.AcquireAsync(GetEndpoint(args.Context), args.Context.CancellationToken),
                OnRejected = args =>
                {
                    Endpoint? endpoint = GetEndpoint(args.Context);
                    double? retryAfterMs = args.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter) ? retryAfter.TotalMilliseconds : null;
                    string path = Describe(endpoint);
                    string rateLimitClass = endpoint?.RateLimit.ToString() ?? nameof(RateLimitClass.None);
                    ResilienceLog.RateLimitRejected(logger, path, rateLimitClass, retryAfterMs);
                    return default;
                },
            });
        }

        // 2. Total timeout across attempts (TotalTimeout).
        builder.AddTimeout(new HttpTimeoutStrategyOptions
        {
            Name = "Smtp2Go.TotalTimeout",
            Timeout = options.TotalTimeout,
            OnTimeout = args =>
            {
                string endpoint = Describe(GetEndpoint(args.Context));
                ResilienceLog.PipelineTimeout(logger, endpoint, "call", args.Timeout.TotalMilliseconds);
                return default;
            },
        });

        // 3. Retry, only for idempotent endpoints unless opted in (MaxRetries, RetryBaseDelay, RetryOnSendEndpoints).
        if (options.MaxRetries > 0)
        {
            builder.AddRetry(new HttpRetryStrategyOptions
            {
                Name = "Smtp2Go.Retry",
                MaxRetryAttempts = options.MaxRetries,
                Delay = options.RetryBaseDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldRetryAfterHeader = true,
                ShouldHandle = args => new ValueTask<bool>(MayRetry(args.Context, options.RetryOnSendEndpoints) && IsTransient(args.Outcome)),
                OnRetry = args =>
                {
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        string endpoint = Describe(GetEndpoint(args.Context));
                        string outcome = Describe(args.Outcome);
                        ResilienceLog.Retrying(logger, endpoint, args.AttemptNumber + 1, outcome, args.RetryDelay.TotalMilliseconds);
                    }

                    return default;
                },
            });
        }

        // 4. Circuit breaker on 5xx and transport failures only (CircuitBreaker.Enabled, FailureRatio, MinimumThroughput, SamplingDuration, BreakDuration).
        if (options.CircuitBreaker.Enabled)
        {
            builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                Name = "Smtp2Go.CircuitBreaker",
                FailureRatio = options.CircuitBreaker.FailureRatio,
                MinimumThroughput = options.CircuitBreaker.MinimumThroughput,
                SamplingDuration = options.CircuitBreaker.SamplingDuration,
                BreakDuration = options.CircuitBreaker.BreakDuration,
                ShouldHandle = args => new ValueTask<bool>(IsServerOrTransportFailure(args.Outcome)),
                OnOpened = args =>
                {
                    if (logger.IsEnabled(LogLevel.Warning))
                    {
                        string endpoint = Describe(GetEndpoint(args.Context));
                        string outcome = Describe(args.Outcome);
                        ResilienceLog.CircuitOpened(logger, args.BreakDuration.TotalMilliseconds, endpoint, outcome);
                    }

                    return default;
                },
                OnClosed = args =>
                {
                    string endpoint = Describe(GetEndpoint(args.Context));
                    ResilienceLog.CircuitClosed(logger, endpoint);
                    return default;
                },
                OnHalfOpened = _ =>
                {
                    ResilienceLog.CircuitHalfOpened(logger);
                    return default;
                },
            });
        }

        // 5. Attempt timeout (AttemptTimeout); a timed-out attempt is a TimeoutRejectedException, handled by retry and circuit breaker above.
        builder.AddTimeout(new HttpTimeoutStrategyOptions
        {
            Name = "Smtp2Go.AttemptTimeout",
            Timeout = options.AttemptTimeout,
            OnTimeout = args =>
            {
                string endpoint = Describe(GetEndpoint(args.Context));
                ResilienceLog.PipelineTimeout(logger, endpoint, "attempt", args.Timeout.TotalMilliseconds);
                return default;
            },
        });
    }

    /// <summary>Whether the request may be retried at all: the endpoint is idempotent, sends are opted in globally, or the caller set <see cref="RequestOptions.AllowRetry"/>.</summary>
    internal static bool MayRetry(ResilienceContext context, bool retryOnSendEndpoints)
    {
        HttpRequestMessage? request = context.GetRequestMessage();
        if (request is null)
        {
            return false;
        }

        if (RequestOptionKeys.TryGetEndpoint(request, out Endpoint? endpoint))
        {
            return endpoint.Idempotent || retryOnSendEndpoints || RequestOptionKeys.GetAllowRetry(request);
        }

        // Not sent by the core transport: only the safe HTTP methods are retried.
        return request.Method == HttpMethod.Get || request.Method == HttpMethod.Head || request.Method == HttpMethod.Options;
    }

    /// <summary>429, 408, 500, 502, 503, 504, transport exceptions and attempt timeouts. Never 400, 401, 402, 403 or 404.</summary>
    internal static bool IsTransient(Outcome<HttpResponseMessage> outcome)
    {
        if (outcome.Exception is not null)
        {
            return outcome.Exception is HttpRequestException or TimeoutRejectedException;
        }

        return outcome.Result?.StatusCode is HttpStatusCode.TooManyRequests
            or HttpStatusCode.RequestTimeout
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    /// <summary>5xx responses, transport exceptions and attempt timeouts. 4xx (including 429) never counts.</summary>
    internal static bool IsServerOrTransportFailure(Outcome<HttpResponseMessage> outcome)
    {
        if (outcome.Exception is not null)
        {
            return outcome.Exception is HttpRequestException or TimeoutRejectedException;
        }

        return outcome.Result is { } response && (int)response.StatusCode >= 500;
    }

    private static Endpoint? GetEndpoint(ResilienceContext context)
    {
        HttpRequestMessage? request = context.GetRequestMessage();
        return request is not null && RequestOptionKeys.TryGetEndpoint(request, out Endpoint? endpoint) ? endpoint : null;
    }

    private static string Describe(Endpoint? endpoint)
    {
        return endpoint?.Path ?? UnknownEndpoint;
    }

    private static string Describe(Outcome<HttpResponseMessage> outcome)
    {
        if (outcome.Exception is { } exception)
        {
            return exception.GetType().Name;
        }

        return outcome.Result is { } response ? "HTTP " + ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture) : "no response";
    }
}
