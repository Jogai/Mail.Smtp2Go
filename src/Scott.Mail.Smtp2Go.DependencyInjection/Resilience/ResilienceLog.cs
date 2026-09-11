using Microsoft.Extensions.Logging;

namespace Scott.Mail.Smtp2Go.DependencyInjection;

/// <summary>Log messages of the resilience pipeline, category <c>Scott.Mail.Smtp2Go.Resilience</c>. Endpoint and outcome only; never the key or a body.</summary>
internal static partial class ResilienceLog
{
    public const string CategoryName = "Scott.Mail.Smtp2Go.Resilience";

    [LoggerMessage(EventId = Smtp2GoEventIds.Retrying, Level = LogLevel.Information, Message = "SMTP2GO {Endpoint} attempt {Attempt} failed ({Outcome}); retrying in {DelayMs} ms")]
    public static partial void Retrying(ILogger logger, string endpoint, int attempt, string outcome, double delayMs);

    [LoggerMessage(EventId = Smtp2GoEventIds.CircuitOpened, Level = LogLevel.Warning, Message = "SMTP2GO circuit opened for {BreakDurationMs} ms after {Endpoint} failed ({Outcome})")]
    public static partial void CircuitOpened(ILogger logger, double breakDurationMs, string endpoint, string outcome);

    [LoggerMessage(EventId = Smtp2GoEventIds.CircuitClosed, Level = LogLevel.Information, Message = "SMTP2GO circuit closed after {Endpoint} succeeded")]
    public static partial void CircuitClosed(ILogger logger, string endpoint);

    [LoggerMessage(EventId = Smtp2GoEventIds.CircuitHalfOpened, Level = LogLevel.Debug, Message = "SMTP2GO circuit half-open; letting one probe request through")]
    public static partial void CircuitHalfOpened(ILogger logger);

    [LoggerMessage(EventId = Smtp2GoEventIds.RateLimitRejected, Level = LogLevel.Warning, Message = "SMTP2GO {Endpoint} rejected by the client-side rate limiter ({RateLimitClass}); retry after {RetryAfterMs} ms")]
    public static partial void RateLimitRejected(ILogger logger, string endpoint, string rateLimitClass, double? retryAfterMs);

    [LoggerMessage(EventId = Smtp2GoEventIds.PipelineTimeout, Level = LogLevel.Warning, Message = "SMTP2GO {Endpoint} {Scope} timed out after {TimeoutMs} ms")]
    public static partial void PipelineTimeout(ILogger logger, string endpoint, string scope, double timeoutMs);
}
