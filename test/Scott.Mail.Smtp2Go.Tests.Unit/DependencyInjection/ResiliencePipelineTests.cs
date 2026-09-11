using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Scott.Mail.Smtp2Go.DependencyInjection;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.DependencyInjection;

public sealed class ResiliencePipelineTests
{
    private const string Idempotent = "email/search";
    private const string Send = "email/send";
    private const string Ok = """{"request_id":"ok-1","data":{}}""";
    private const string Down = """{"request_id":"down-1","data":{"error":"unavailable","error_code":"E_ApiResponseCodes.API_ERROR"}}""";

    [Fact]
    public async Task Idempotent_endpoint_is_retried_on_503_then_succeeds()
    {
        using DiTestHost host = DiTestHost.Create();
        int calls = 0;
        host.Handler.Respond(Idempotent, (_, _) => Task.FromResult(FakeHttpMessageHandler.Json(++calls == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK, calls == 1 ? Down : Ok)));

        using JsonDocument document = await host.RunAsync(() => host.Client().Raw.SendJsonAsync(Idempotent, default, cancellationToken: TestContext.Current.CancellationToken));

        calls.Should().Be(2);
        document.RootElement.GetProperty("request_id").GetString().Should().Be("ok-1");
        host.Logs.Entries.Should().Contain(e => e.EventId.Id == Smtp2GoEventIds.Retrying && e.Message.Contains(Idempotent) && e.Message.Contains("HTTP 503"));
    }

    [Fact]
    public async Task Send_endpoint_is_not_retried()
    {
        using DiTestHost host = DiTestHost.Create();
        host.Handler.Respond(Send, HttpStatusCode.ServiceUnavailable, Down);

        Func<Task> act = () => host.RunAsync(() => host.Client().Raw.SendJsonAsync(Send, default, cancellationToken: TestContext.Current.CancellationToken));

        (await act.Should().ThrowAsync<Smtp2GoApiException>()).Which.StatusCode.Should().Be(503);
        host.Handler.Requests.Should().HaveCount(1);
        host.Logs.Entries.Should().NotContain(e => e.EventId.Id == Smtp2GoEventIds.Retrying);
    }

    [Fact]
    public async Task RetryOnSendEndpoints_opts_sends_in()
    {
        using DiTestHost host = DiTestHost.Create(options => options.Resilience.RetryOnSendEndpoints = true);
        int calls = 0;
        host.Handler.Respond(Send, (_, _) => Task.FromResult(FakeHttpMessageHandler.Json(++calls == 1 ? HttpStatusCode.BadGateway : HttpStatusCode.OK, calls == 1 ? Down : Ok)));

        using JsonDocument document = await host.RunAsync(() => host.Client().Raw.SendJsonAsync(Send, default, cancellationToken: TestContext.Current.CancellationToken));

        calls.Should().Be(2);
    }

    [Fact]
    public async Task AllowRetry_opts_a_single_send_in()
    {
        using DiTestHost host = DiTestHost.Create();
        int calls = 0;
        host.Handler.Respond(Send, (_, _) => Task.FromResult(FakeHttpMessageHandler.Json(++calls == 1 ? HttpStatusCode.GatewayTimeout : HttpStatusCode.OK, calls == 1 ? Down : Ok)));

        using JsonDocument document = await host.RunAsync(() => host.Client().Raw.SendJsonAsync(
            Send, default, options: new RequestOptions { AllowRetry = true }, cancellationToken: TestContext.Current.CancellationToken));

        calls.Should().Be(2);
        host.Handler.Requests.Should().AllSatisfy(r => r.AllowRetry.Should().BeTrue());
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.PaymentRequired)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task Client_errors_are_never_retried(HttpStatusCode status)
    {
        using DiTestHost host = DiTestHost.Create(options => options.Resilience.RetryOnSendEndpoints = true);
        host.Handler.Respond(Idempotent, status, Down);

        Func<Task> act = () => host.RunAsync(() => host.Client().Raw.SendJsonAsync(Idempotent, default, cancellationToken: TestContext.Current.CancellationToken));

        (await act.Should().ThrowAsync<Smtp2GoApiException>()).Which.StatusCode.Should().Be((int)status);
        host.Handler.Requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task Retry_can_be_disabled()
    {
        using DiTestHost host = DiTestHost.Create(options => options.Resilience.MaxRetries = 0);
        host.Handler.Respond(Idempotent, HttpStatusCode.ServiceUnavailable, Down);

        Func<Task> act = () => host.RunAsync(() => host.Client().Raw.SendJsonAsync(Idempotent, default, cancellationToken: TestContext.Current.CancellationToken));

        await act.Should().ThrowAsync<Smtp2GoApiException>();
        host.Handler.Requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task TooManyRequests_waits_for_retry_after()
    {
        using DiTestHost host = DiTestHost.Create();
        List<DateTimeOffset> attempts = [];
        host.Handler.Respond(Idempotent, (_, _) =>
        {
            attempts.Add(host.Time.GetUtcNow());
            if (attempts.Count == 1)
            {
                HttpResponseMessage limited = FakeHttpMessageHandler.Json(HttpStatusCode.TooManyRequests, Down);
                limited.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(7));
                return Task.FromResult(limited);
            }

            return Task.FromResult(FakeHttpMessageHandler.Json(HttpStatusCode.OK, Ok));
        });

        using JsonDocument document = await host.RunAsync(() => host.Client().Raw.SendJsonAsync(Idempotent, default, cancellationToken: TestContext.Current.CancellationToken));

        attempts.Should().HaveCount(2);
        (attempts[1] - attempts[0]).Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(7));
        host.Logs.Entries.Should().Contain(e => e.EventId.Id == Smtp2GoEventIds.Retrying && e.Message.Contains("HTTP 429") && e.Message.Contains("7000 ms"));
    }

    [Fact]
    public async Task Circuit_opens_after_the_failure_threshold()
    {
        using DiTestHost host = DiTestHost.Create(options =>
        {
            options.Resilience.MaxRetries = 0;
            options.Resilience.CircuitBreaker.MinimumThroughput = 2;
            options.Resilience.CircuitBreaker.FailureRatio = 0.5;
        });
        host.Handler.Respond(Idempotent, HttpStatusCode.InternalServerError, Down);
        ISmtp2GoClient client = host.Client();

        Func<Task> call = () => host.RunAsync(() => client.Raw.SendJsonAsync(Idempotent, default, cancellationToken: TestContext.Current.CancellationToken));

        await call.Should().ThrowAsync<Smtp2GoApiException>();
        await call.Should().ThrowAsync<Smtp2GoApiException>();
        await call.Should().ThrowAsync<BrokenCircuitException>();
        host.Handler.Requests.Should().HaveCount(2);
        host.Logs.Entries.Should().Contain(e => e.EventId.Id == Smtp2GoEventIds.CircuitOpened && e.Message.Contains("HTTP 500"));
    }

    [Fact]
    public async Task Client_errors_do_not_trip_the_circuit()
    {
        using DiTestHost host = DiTestHost.Create(options =>
        {
            options.Resilience.MaxRetries = 0;
            options.Resilience.CircuitBreaker.MinimumThroughput = 2;
            options.Resilience.CircuitBreaker.FailureRatio = 0.5;
        });
        host.Handler.Respond(Idempotent, HttpStatusCode.NotFound, Down);
        ISmtp2GoClient client = host.Client();

        Func<Task> call = () => host.RunAsync(() => client.Raw.SendJsonAsync(Idempotent, default, cancellationToken: TestContext.Current.CancellationToken));

        for (int i = 0; i < 4; i++)
        {
            await call.Should().ThrowAsync<Smtp2GoApiException>();
        }

        host.Handler.Requests.Should().HaveCount(4);
        host.Logs.Entries.Should().NotContain(e => e.EventId.Id == Smtp2GoEventIds.CircuitOpened);
    }

    [Fact]
    public async Task Circuit_can_be_disabled()
    {
        using DiTestHost host = DiTestHost.Create(options =>
        {
            options.Resilience.MaxRetries = 0;
            options.Resilience.CircuitBreaker.Enabled = false;
            options.Resilience.CircuitBreaker.MinimumThroughput = 2;
        });
        host.Handler.Respond(Idempotent, HttpStatusCode.InternalServerError, Down);
        ISmtp2GoClient client = host.Client();

        Func<Task> call = () => host.RunAsync(() => client.Raw.SendJsonAsync(Idempotent, default, cancellationToken: TestContext.Current.CancellationToken));

        for (int i = 0; i < 4; i++)
        {
            await call.Should().ThrowAsync<Smtp2GoApiException>();
        }

        host.Handler.Requests.Should().HaveCount(4);
    }

    [Fact]
    public async Task Attempt_timeout_is_retried_as_a_transient_failure()
    {
        using DiTestHost host = DiTestHost.Create(options => options.Resilience.AttemptTimeout = TimeSpan.FromSeconds(5));
        int calls = 0;
        host.Handler.Respond(Idempotent, async (_, ct) =>
        {
            if (++calls == 1)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            }

            return FakeHttpMessageHandler.Json(HttpStatusCode.OK, Ok);
        });

        using JsonDocument document = await host.RunAsync(() => host.Client().Raw.SendJsonAsync(Idempotent, default, cancellationToken: TestContext.Current.CancellationToken));

        calls.Should().Be(2);
        host.Logs.Entries.Should().Contain(e => e.EventId.Id == Smtp2GoEventIds.PipelineTimeout && e.Message.Contains("attempt") && e.Message.Contains("5000 ms"));
        host.Logs.Entries.Should().Contain(e => e.EventId.Id == Smtp2GoEventIds.Retrying && e.Message.Contains("TimeoutRejectedException"));
    }

    [Fact]
    public async Task Rate_limiter_delays_requests_over_the_window()
    {
        TimeSpan window = TimeSpan.FromMilliseconds(400);
        using DiTestHost host = DiTestHost.Create(options =>
            options.Resilience.RateLimiting.Overrides[RateLimitClass.EmailSearch] = new RateLimitWindow { PermitLimit = 2, Window = window, QueueLimit = 10 });
        ISmtp2GoClient client = host.Client();
        Stopwatch stopwatch = Stopwatch.StartNew();
        List<TimeSpan> arrivals = [];
        host.Handler.Respond(Idempotent, (_, _) =>
        {
            lock (arrivals)
            {
                arrivals.Add(stopwatch.Elapsed);
            }

            return Task.FromResult(FakeHttpMessageHandler.Json(HttpStatusCode.OK, Ok));
        });

        JsonDocument[] documents = await Task.WhenAll(
            Enumerable.Range(0, 3).Select(_ => client.Raw.SendJsonAsync(Idempotent, default, cancellationToken: TestContext.Current.CancellationToken)));
        foreach (JsonDocument document in documents)
        {
            document.Dispose();
        }

        arrivals.Sort();
        arrivals.Should().HaveCount(3);
        arrivals[1].Should().BeLessThan(window);
        arrivals[2].Should().BeGreaterThanOrEqualTo(window - TimeSpan.FromMilliseconds(50), "the third request waits for the window to slide");
    }

    [Fact]
    public async Task Rate_limiter_rejects_when_the_queue_is_full()
    {
        using DiTestHost host = DiTestHost.Create(options =>
            options.Resilience.RateLimiting.Overrides[RateLimitClass.EmailSearch] = new RateLimitWindow { PermitLimit = 1, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
        ISmtp2GoClient client = host.Client();
        host.Handler.Respond(Idempotent, HttpStatusCode.OK, Ok);

        using JsonDocument first = await client.Raw.SendJsonAsync(Idempotent, default, cancellationToken: TestContext.Current.CancellationToken);
        Func<Task> second = () => client.Raw.SendJsonAsync(Idempotent, default, cancellationToken: TestContext.Current.CancellationToken);

        await second.Should().ThrowAsync<RateLimiterRejectedException>();
        host.Handler.Requests.Should().HaveCount(1);
        host.Logs.Entries.Should().Contain(e => e.EventId.Id == Smtp2GoEventIds.RateLimitRejected && e.Message.Contains(Idempotent) && e.Message.Contains(nameof(RateLimitClass.EmailSearch)));
    }

    [Fact]
    public async Task Rate_limiting_can_be_disabled()
    {
        using DiTestHost host = DiTestHost.Create(options =>
        {
            options.Resilience.RateLimiting.Enabled = false;
            options.Resilience.RateLimiting.Overrides[RateLimitClass.EmailSearch] = new RateLimitWindow { PermitLimit = 1, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 };
        });
        ISmtp2GoClient client = host.Client();
        host.Handler.Respond(Idempotent, HttpStatusCode.OK, Ok);

        using JsonDocument first = await client.Raw.SendJsonAsync(Idempotent, default, cancellationToken: TestContext.Current.CancellationToken);
        using JsonDocument second = await client.Raw.SendJsonAsync(Idempotent, default, cancellationToken: TestContext.Current.CancellationToken);

        host.Handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task ActivitySearch_limiter_queues_the_61st_request()
    {
        // The leases are held for the whole test, so the global concurrency cap must not be the limit that is hit.
        using EndpointRateLimiters limiters = new(new RateLimitingOptions { GlobalConcurrency = 100 });
        Endpoint activitySearch = new("activity/search", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: true, RateLimitClass.ActivitySearch, Endpoint.DefaultMaxBodyBytes);
        List<RateLimitLease> leases = [];

        for (int i = 0; i < 60; i++)
        {
            RateLimitLease lease = await limiters.AcquireAsync(activitySearch, TestContext.Current.CancellationToken);
            lease.IsAcquired.Should().BeTrue($"request {i + 1} is within the documented 60 per minute");
            leases.Add(lease);
        }

        ValueTask<RateLimitLease> sixtyFirst = limiters.AcquireAsync(activitySearch, TestContext.Current.CancellationToken);

        sixtyFirst.IsCompleted.Should().BeFalse("the 61st request waits for the window to slide");
        foreach (RateLimitLease lease in leases)
        {
            lease.Dispose();
        }

        limiters.GetLimiter(RateLimitClass.None).Should().BeNull();
        limiters.GetLimiter(RateLimitClass.ApiKeyAdd).Should().NotBeNull();
        EndpointRateLimiters.DocumentedWindows.Should().HaveCount(4);
    }

    [Fact]
    public async Task Unlimited_classes_only_take_the_global_concurrency_permit()
    {
        using EndpointRateLimiters limiters = new(new RateLimitingOptions { GlobalConcurrency = 1, GlobalQueueLimit = 0 });
        Endpoint send = EndpointTable.Get(Send);

        using RateLimitLease first = await limiters.AcquireAsync(send, TestContext.Current.CancellationToken);
        RateLimitLease second = await limiters.AcquireAsync(send, TestContext.Current.CancellationToken);

        first.IsAcquired.Should().BeTrue();
        second.IsAcquired.Should().BeFalse("only one request may be in flight");
        second.Dispose();
        first.Dispose();

        using RateLimitLease third = await limiters.AcquireAsync(send, TestContext.Current.CancellationToken);
        third.IsAcquired.Should().BeTrue("disposing the lease released the concurrency permit");
    }

    [Fact]
    public void Every_resilience_option_is_consumed_by_the_pipeline()
    {
        string root = FindRepositoryRoot();
        string resilienceFolder = Path.Combine(root, "src", "Scott.Mail.Smtp2Go.DependencyInjection", "Resilience");
        string source = string.Join(
            "\n",
            Directory.GetFiles(resilienceFolder, "*.cs")
                .Where(f => !Path.GetFileName(f).EndsWith("Options.cs", StringComparison.Ordinal) && !Path.GetFileName(f).Equals("RateLimitWindow.cs", StringComparison.Ordinal))
                .Select(File.ReadAllText)
                .Select(StripComments));

        List<string> unconsumed = [];
        foreach ((Type type, PropertyInfo property) in OptionProperties(typeof(ResilienceOptions)))
        {
            if (!Regex.IsMatch(source, $@"\b{Regex.Escape(property.Name)}\b"))
            {
                unconsumed.Add($"{type.Name}.{property.Name}");
            }
        }

        unconsumed.Should().BeEmpty("every ResilienceOptions property must be read by Smtp2GoResilienceBuilder or EndpointRateLimiters");
        OptionProperties(typeof(ResilienceOptions)).Should().HaveCountGreaterThan(15, "the walk must cover the nested option types");
    }

    private static IEnumerable<(Type Type, PropertyInfo Property)> OptionProperties(Type type)
    {
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            yield return (type, property);
            Type propertyType = property.PropertyType;
            if (propertyType.IsGenericType)
            {
                propertyType = propertyType.GetGenericArguments()[^1];
            }

            if (propertyType.Assembly == typeof(ResilienceOptions).Assembly && propertyType.IsClass)
            {
                foreach ((Type, PropertyInfo) nested in OptionProperties(propertyType))
                {
                    yield return nested;
                }
            }
        }
    }

    private static string StripComments(string source)
    {
        return Regex.Replace(source, @"(//[^\n]*)|(/\*.*?\*/)", string.Empty, RegexOptions.Singleline);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Scott.Mail.Smtp2Go.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not find the repository root above " + AppContext.BaseDirectory);
    }
}
