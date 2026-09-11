using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace Scott.Mail.Smtp2Go.Tests.AspNetCore;

public class SourceIpTests
{
    private const string Fixture = "Docs/delivered.json";
    private static readonly IPAddress s_smtp2Go = IPAddress.Parse("203.0.113.10");
    private static readonly IPAddress s_smtp2GoV6 = IPAddress.Parse("2001:db8::10");
    private static readonly IPAddress s_stranger = IPAddress.Parse("198.51.100.7");

    private static Task<WebhookTestHost> StartAsync(IPAddress? remoteIp, StubResolver? resolver = null)
    {
        resolver ??= new StubResolver(s_smtp2Go, s_smtp2GoV6);
        return WebhookTestHost.StartAsync(
            services => services.AddSingleton<ISmtp2GoSourceIpResolver>(resolver),
            (test, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path, test.RecordAsync).RequireSmtp2GoSourceIp(),
            remoteIp);
    }

    [Fact]
    public async Task Request_from_a_resolved_address_is_accepted()
    {
        await using WebhookTestHost host = await StartAsync(s_smtp2Go);

        using HttpResponseMessage response = await host.PostFixtureAsync(Fixture);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        host.Received.Should().ContainSingle();
    }

    [Fact]
    public async Task Request_from_a_resolved_ipv6_address_is_accepted()
    {
        await using WebhookTestHost host = await StartAsync(s_smtp2GoV6);

        using HttpResponseMessage response = await host.PostFixtureAsync(Fixture);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ipv4_mapped_ipv6_remote_address_matches_the_ipv4_record()
    {
        await using WebhookTestHost host = await StartAsync(s_smtp2Go.MapToIPv6());

        using HttpResponseMessage response = await host.PostFixtureAsync(Fixture);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Request_from_another_address_gets_403_before_authentication_and_the_body()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(
            services => services.AddSingleton<ISmtp2GoSourceIpResolver>(new StubResolver(s_smtp2Go)),
            (test, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path, test.RecordAsync).RequireBearer("tok").RequireSmtp2GoSourceIp(),
            s_stranger);

        using HttpResponseMessage response = await host.PostAsync("nope", "text/plain");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.WwwAuthenticate.Should().BeEmpty();
        host.Received.Should().BeEmpty();
        CapturingLoggerProvider.LogEntry entry = host.Logs.Smtp2Go.Should().ContainSingle(e => e.EventId.Id == Smtp2GoWebhookEventIds.SourceIpRejected).Subject;
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Message.Should().Contain("198.51.100.7").And.Contain("webhooks.smtp2go.com");
    }

    [Fact]
    public async Task Unknown_remote_address_gets_403()
    {
        await using WebhookTestHost host = await StartAsync(remoteIp: null);

        using HttpResponseMessage response = await host.PostFixtureAsync(Fixture);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        host.Logs.Smtp2Go.Should().ContainSingle(e => e.EventId.Id == Smtp2GoWebhookEventIds.SourceIpRejected).Which.Message.Should().Contain("unknown");
    }

    [Fact]
    public async Task Resolver_failure_gets_503_and_an_error_log()
    {
        await using WebhookTestHost host = await StartAsync(s_smtp2Go, new StubResolver { Failure = new InvalidOperationException("dns down") });

        using HttpResponseMessage response = await host.PostFixtureAsync(Fixture);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        host.Received.Should().BeEmpty();
        CapturingLoggerProvider.LogEntry entry = host.Logs.Smtp2Go.Should().ContainSingle(e => e.EventId.Id == Smtp2GoWebhookEventIds.SourceIpResolutionFailed).Subject;
        entry.Level.Should().Be(LogLevel.Error);
        entry.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task Check_is_off_by_default()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(
            services => services.AddSingleton<ISmtp2GoSourceIpResolver>(new StubResolver(s_smtp2Go)),
            remoteIp: s_stranger);

        using HttpResponseMessage response = await host.PostFixtureAsync(Fixture);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Resolver_result_is_cached_for_an_hour_on_the_time_provider()
    {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 9, 11, 12, 0, 0, TimeSpan.Zero));
        CountingResolver resolver = new(time, [s_smtp2Go]);

        (await resolver.GetAddressesAsync(TestContext.Current.CancellationToken)).Should().Equal(s_smtp2Go);
        time.Advance(TimeSpan.FromMinutes(59));
        (await resolver.GetAddressesAsync(TestContext.Current.CancellationToken)).Should().Equal(s_smtp2Go);
        resolver.Lookups.Should().Be(1);

        resolver.Next = [s_smtp2GoV6];
        time.Advance(TimeSpan.FromMinutes(2));
        (await resolver.GetAddressesAsync(TestContext.Current.CancellationToken)).Should().Equal(s_smtp2GoV6);
        resolver.Lookups.Should().Be(2);
    }

    [Fact]
    public async Task Concurrent_callers_share_one_lookup()
    {
        FakeTimeProvider time = new();
        CountingResolver resolver = new(time, [s_smtp2Go]) { Delay = new TaskCompletionSource() };

        Task<IReadOnlyCollection<IPAddress>>[] calls = [.. Enumerable.Range(0, 5).Select(_ => resolver.GetAddressesAsync(TestContext.Current.CancellationToken).AsTask())];
        calls.Should().OnlyContain(call => !call.IsCompleted);
        resolver.Delay.SetResult();
        IReadOnlyCollection<IPAddress>[] results = await Task.WhenAll(calls);

        results.Should().OnlyContain(result => result.Single().Equals(s_smtp2Go));
        resolver.Lookups.Should().Be(1);
    }

    [Fact]
    public async Task Failed_refresh_serves_the_stale_result_and_retries_a_minute_later()
    {
        FakeTimeProvider time = new();
        CapturingLoggerProvider logs = new();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(logging => logging.SetMinimumLevel(LogLevel.Trace).AddProvider(logs));
        CountingResolver resolver = new(time, [s_smtp2Go], loggerFactory);
        await resolver.GetAddressesAsync(TestContext.Current.CancellationToken);

        time.Advance(TimeSpan.FromHours(2));
        resolver.Failure = new InvalidOperationException("dns down");
        (await resolver.GetAddressesAsync(TestContext.Current.CancellationToken)).Should().Equal(s_smtp2Go);
        time.Advance(TimeSpan.FromSeconds(30));
        (await resolver.GetAddressesAsync(TestContext.Current.CancellationToken)).Should().Equal(s_smtp2Go);
        resolver.Lookups.Should().Be(2, "the stale result is reused for a minute before DNS is tried again");

        resolver.Failure = null;
        resolver.Next = [s_smtp2GoV6];
        time.Advance(TimeSpan.FromSeconds(31));
        (await resolver.GetAddressesAsync(TestContext.Current.CancellationToken)).Should().Equal(s_smtp2GoV6);
        resolver.Lookups.Should().Be(3);
        logs.Smtp2Go.Should().ContainSingle(e => e.EventId.Id == Smtp2GoWebhookEventIds.SourceIpStaleCacheUsed).Which.Level.Should().Be(LogLevel.Warning);
        logs.Smtp2Go.Where(e => e.EventId.Id == Smtp2GoWebhookEventIds.SourceIpResolved).Should().HaveCount(2);
    }

    [Fact]
    public async Task Failure_without_a_cached_result_propagates_and_the_next_call_tries_again()
    {
        FakeTimeProvider time = new();
        CountingResolver resolver = new(time, [s_smtp2Go]) { Failure = new InvalidOperationException("dns down") };

        Func<Task> act = async () => await resolver.GetAddressesAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("dns down");
        resolver.Failure = null;
        (await resolver.GetAddressesAsync(TestContext.Current.CancellationToken)).Should().Equal(s_smtp2Go);
        resolver.Lookups.Should().Be(2);
    }

    [Fact]
    public async Task Empty_lookup_counts_as_a_failure()
    {
        CountingResolver resolver = new(new FakeTimeProvider(), []);

        Func<Task> act = async () => await resolver.GetAddressesAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no addresses*");
    }

    [Fact]
    public async Task Ipv4_mapped_records_are_normalised_to_ipv4()
    {
        CountingResolver resolver = new(new FakeTimeProvider(), [s_smtp2Go.MapToIPv6(), s_smtp2GoV6]);

        (await resolver.GetAddressesAsync(TestContext.Current.CancellationToken)).Should().Equal(s_smtp2Go, s_smtp2GoV6);
    }

    [Fact]
    public void Constants_match_the_documentation()
    {
        Smtp2GoSourceIpResolver.HostName.Should().Be("webhooks.smtp2go.com");
        Smtp2GoSourceIpResolver.CacheDuration.Should().Be(TimeSpan.FromHours(1));
    }

    private sealed class StubResolver(params IPAddress[] addresses) : ISmtp2GoSourceIpResolver
    {
        public Exception? Failure { get; init; }

        public ValueTask<IReadOnlyCollection<IPAddress>> GetAddressesAsync(CancellationToken cancellationToken)
        {
            return Failure is null ? new ValueTask<IReadOnlyCollection<IPAddress>>(addresses) : ValueTask.FromException<IReadOnlyCollection<IPAddress>>(Failure);
        }
    }

    /// <summary>The real resolver with DNS replaced by a scripted lookup.</summary>
    private sealed class CountingResolver(TimeProvider time, IPAddress[] first, ILoggerFactory? loggerFactory = null) : Smtp2GoSourceIpResolver(time, loggerFactory)
    {
        public int Lookups { get; private set; }

        public IPAddress[] Next { get; set; } = first;

        public Exception? Failure { get; set; }

        public TaskCompletionSource? Delay { get; init; }

        protected override async Task<IPAddress[]> LookupAsync(string hostName, CancellationToken cancellationToken)
        {
            hostName.Should().Be(HostName);
            Lookups++;
            if (Delay is not null)
            {
                await Delay.Task;
            }

            return Failure is null ? Next : throw Failure;
        }
    }
}
