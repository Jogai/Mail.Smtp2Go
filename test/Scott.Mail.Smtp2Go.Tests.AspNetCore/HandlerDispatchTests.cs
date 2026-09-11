using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Webhooks;

namespace Scott.Mail.Smtp2Go.Tests.AspNetCore;

public class HandlerDispatchTests
{
    private static readonly WebhookPayloadParser s_parser = WebhookPayloadParser.Default;

    public static TheoryData<string> Fixtures()
    {
        TheoryData<string> data = [];
        foreach (string folder in new[] { "Docs", "Live" })
        {
            foreach (string file in Directory.GetFiles(Fixture.PathOf("Webhooks/" + folder)).Order(StringComparer.Ordinal))
            {
                data.Add(folder + "/" + Path.GetFileName(file));
            }
        }

        return data;
    }

    private static ServiceProvider BuildProvider(Recorder recorder, Action<IServiceCollection> register)
    {
        ServiceCollection services = new();
        services.AddSingleton(recorder);
        services.AddSmtp2GoWebhooks();
        register(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static async Task<int> DispatchAsync(ServiceProvider provider, WebhookEvent webhookEvent)
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IWebhookEventDispatcher>().DispatchAsync(webhookEvent, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Click_walks_concrete_then_open_then_email_then_base_in_that_order()
    {
        Recorder recorder = new();
        await using ServiceProvider provider = BuildProvider(recorder, services =>
        {
            services.AddHandler<WebhookEvent>("base");
            services.AddHandler<EmailWebhookEvent>("email");
            services.AddHandler<EmailOpenEvent>("open");
            services.AddHandler<EmailClickEvent>("click");
            services.AddHandler<EmailBounceEvent>("bounce");
        });

        int invoked = await DispatchAsync(provider, s_parser.Parse(WebhookTestHost.ReadFixture("Live/clicked.json")));

        invoked.Should().Be(4);
        recorder.Calls.Should().Equal("click", "open", "email", "base");
    }

    [Fact]
    public async Task Handlers_of_one_type_run_in_registration_order()
    {
        Recorder recorder = new();
        await using ServiceProvider provider = BuildProvider(recorder, services =>
        {
            services.AddHandler<EmailBounceEvent>("bounce-1");
            services.AddHandler<WebhookEvent>("base-1");
            services.AddHandler<EmailBounceEvent>("bounce-2");
            services.AddHandler<WebhookEvent>("base-2");
        });

        await DispatchAsync(provider, s_parser.Parse(WebhookTestHost.ReadFixture("Docs/bounce.json")));

        recorder.Calls.Should().Equal("bounce-1", "bounce-2", "base-1", "base-2");
    }

    [Fact]
    public async Task Unknown_event_reaches_only_the_unknown_and_base_handlers()
    {
        Recorder recorder = new();
        await using ServiceProvider provider = BuildProvider(recorder, services =>
        {
            services.AddHandler<EmailWebhookEvent>("email");
            services.AddHandler<UnknownWebhookEvent>("unknown");
            services.AddHandler<WebhookEvent>("base");
        });

        int invoked = await DispatchAsync(provider, s_parser.Parse(WebhookTestHost.ReadFixture("Docs/unknown_future.json")));

        invoked.Should().Be(2);
        recorder.Calls.Should().Equal("unknown", "base");
    }

    [Fact]
    public async Task Sms_event_reaches_the_sms_and_base_handlers()
    {
        Recorder recorder = new();
        await using ServiceProvider provider = BuildProvider(recorder, services =>
        {
            services.AddHandler<EmailWebhookEvent>("email");
            services.AddHandler<SmsStatusEvent>("sms");
            services.AddHandler<WebhookEvent>("base");
        });

        await DispatchAsync(provider, s_parser.Parse(WebhookTestHost.ReadFixture("Docs/sms_delivered.json")));

        recorder.Calls.Should().Equal("sms", "base");
    }

    [Fact]
    public async Task Handler_for_another_concrete_type_is_not_invoked()
    {
        Recorder recorder = new();
        await using ServiceProvider provider = BuildProvider(recorder, services => services.AddHandler<EmailBounceEvent>("bounce"));

        int invoked = await DispatchAsync(provider, s_parser.Parse(WebhookTestHost.ReadFixture("Docs/delivered.json")));

        invoked.Should().Be(0);
        recorder.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task A_throwing_handler_stops_the_chain_and_propagates()
    {
        Recorder recorder = new();
        await using ServiceProvider provider = BuildProvider(recorder, services =>
        {
            services.AddHandler<EmailDeliveredEvent>("delivered", throwing: true);
            services.AddHandler<WebhookEvent>("base");
        });

        Func<Task> act = () => DispatchAsync(provider, s_parser.Parse(WebhookTestHost.ReadFixture("Docs/delivered.json")));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("delivered failed");
        recorder.Calls.Should().Equal("delivered");
    }

    [Fact]
    public async Task Cancellation_token_reaches_the_handlers()
    {
        Recorder recorder = new();
        using CancellationTokenSource cts = new();
        await using ServiceProvider provider = BuildProvider(recorder, services => services.AddHandler<WebhookEvent>("base"));
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IWebhookEventDispatcher>().DispatchAsync(s_parser.Parse(WebhookTestHost.ReadFixture("Docs/open.json")), cts.Token);

        recorder.Tokens.Should().ContainSingle().Which.Should().Be(cts.Token);
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public async Task Every_fixture_posted_to_the_dispatching_endpoint_reaches_the_handler_of_its_concrete_type_first(string fixture)
    {
        Recorder recorder = new();
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(
            services =>
            {
                services.AddSingleton(recorder);
                services.AddSmtp2GoWebhooks();
                services.AddHandler<WebhookEvent>(nameof(WebhookEvent));
                services.AddHandler<EmailWebhookEvent>(nameof(EmailWebhookEvent));
                services.AddHandler<EmailProcessedEvent>(nameof(EmailProcessedEvent));
                services.AddHandler<EmailDeliveredEvent>(nameof(EmailDeliveredEvent));
                services.AddHandler<EmailBounceEvent>(nameof(EmailBounceEvent));
                services.AddHandler<EmailOpenEvent>(nameof(EmailOpenEvent));
                services.AddHandler<EmailClickEvent>(nameof(EmailClickEvent));
                services.AddHandler<EmailSpamEvent>(nameof(EmailSpamEvent));
                services.AddHandler<EmailUnsubscribeEvent>(nameof(EmailUnsubscribeEvent));
                services.AddHandler<EmailResubscribeEvent>(nameof(EmailResubscribeEvent));
                services.AddHandler<EmailRejectEvent>(nameof(EmailRejectEvent));
                services.AddHandler<SmsStatusEvent>(nameof(SmsStatusEvent));
                services.AddHandler<UnknownWebhookEvent>(nameof(UnknownWebhookEvent));
            },
            (_, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path));

        using HttpResponseMessage response = await host.PostFixtureAsync(fixture);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        WebhookEvent received = recorder.Events.Should().NotBeEmpty().And.Subject.First();
        recorder.Calls.First().Should().Be(received.GetType().Name, "the concrete handler runs first");
        recorder.Calls.Last().Should().Be(nameof(WebhookEvent), "the catch-all runs last");
        recorder.Events.Should().OnlyContain(e => ReferenceEquals(e, received), "every handler sees the same instance");
    }

    [Fact]
    public async Task Endpoint_without_a_matching_handler_still_returns_200_and_logs_at_debug()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(services => services.AddSmtp2GoWebhooks(), (_, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path));

        using HttpResponseMessage response = await host.PostFixtureAsync("Docs/delivered.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CapturingLoggerProvider.LogEntry entry = host.Logs.Smtp2Go.Should().ContainSingle(e => e.EventId.Id == Smtp2GoWebhookEventIds.NoHandlerRegistered).Subject;
        entry.Level.Should().Be(LogLevel.Debug);
        entry.Message.Should().Contain("EmailDelivered").And.Contain("EmailDeliveredEvent");
    }

    [Fact]
    public async Task Handler_exception_through_the_endpoint_returns_the_error_status()
    {
        Recorder recorder = new();
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(
            services =>
            {
                services.AddSingleton(recorder);
                services.AddSmtp2GoWebhooks();
                services.AddHandler<EmailWebhookEvent>("email", throwing: true);
            },
            (_, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path));

        using HttpResponseMessage response = await host.PostFixtureAsync("Docs/delivered.json");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        host.Logs.Smtp2Go.Should().ContainSingle(e => e.EventId.Id == Smtp2GoWebhookEventIds.HandlerFailed).Which.Exception!.Message.Should().Be("email failed");
    }

    [Fact]
    public async Task Scoped_handlers_get_a_fresh_instance_per_request()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(
            services =>
            {
                services.AddSmtp2GoWebhooks();
                services.AddScoped<IWebhookEventHandler<WebhookEvent>, ScopedHandler>();
            },
            (_, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path));

        using HttpResponseMessage first = await host.PostFixtureAsync("Docs/delivered.json");
        using HttpResponseMessage second = await host.PostFixtureAsync("Docs/delivered.json");

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        ScopedHandler.Instances.Should().HaveCount(2);
        ScopedHandler.Instances.Distinct().Should().HaveCount(2);
    }

    [Fact]
    public async Task Dispatching_endpoint_carries_the_dispatch_flag_in_its_metadata()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(services => services.AddSmtp2GoWebhooks(), (_, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path));

        Endpoint endpoint = host.Services.GetRequiredService<EndpointDataSource>().Endpoints.Should().ContainSingle().Subject;

        endpoint.Metadata.GetMetadata<Smtp2GoWebhookMetadata>()!.UsesHandlerDispatch.Should().BeTrue();
    }

    [Fact]
    public void Mapping_without_AddSmtp2GoWebhooks_fails_at_startup()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        using WebApplication app = builder.Build();

        Action act = () => app.MapSmtp2GoWebhook(WebhookTestHost.Path);

        act.Should().Throw<InvalidOperationException>().WithMessage("*AddSmtp2GoWebhooks*").WithMessage($"*{WebhookTestHost.Path}*");
    }

    [Fact]
    public void AddSmtp2GoWebhooks_registers_options_dispatcher_and_resolver_once_and_keeps_a_custom_resolver()
    {
        ServiceCollection services = new();
        StubResolver custom = new();
        services.AddSingleton<ISmtp2GoSourceIpResolver>(custom);
        services.AddSmtp2GoWebhooks(options => options.MaxBodyBytes = 2048);
        services.AddSmtp2GoWebhooks();
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        provider.GetRequiredService<IOptions<Smtp2GoWebhookOptions>>().Value.MaxBodyBytes.Should().Be(2048);
        provider.GetRequiredService<ISmtp2GoSourceIpResolver>().Should().BeSameAs(custom);
        services.Where(descriptor => descriptor.ServiceType == typeof(IWebhookEventDispatcher)).Should().ContainSingle().Which.Lifetime.Should().Be(ServiceLifetime.Scoped);
        services.Where(descriptor => descriptor.ServiceType == typeof(ISmtp2GoSourceIpResolver)).Should().ContainSingle();
        using IServiceScope scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IWebhookEventDispatcher>().Should().NotBeNull();
    }

    [Fact]
    public void AddSmtp2GoWebhooks_default_resolver_uses_the_container_time_provider()
    {
        ServiceCollection services = new();
        services.AddSmtp2GoWebhooks();
        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ISmtp2GoSourceIpResolver>().Should().BeOfType<Smtp2GoSourceIpResolver>();
    }

    [Fact]
    public async Task Dispatcher_rejects_a_null_event()
    {
        await using ServiceProvider provider = BuildProvider(new Recorder(), _ => { });
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        Func<Task> act = () => scope.ServiceProvider.GetRequiredService<IWebhookEventDispatcher>().DispatchAsync(null!, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    internal sealed class Recorder
    {
        public List<string> Calls { get; } = [];

        public List<WebhookEvent> Events { get; } = [];

        public List<CancellationToken> Tokens { get; } = [];
    }

    private sealed class ScopedHandler : IWebhookEventHandler<WebhookEvent>
    {
        public static List<ScopedHandler> Instances { get; } = [];

        public Task HandleAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
        {
            Instances.Add(this);
            return Task.CompletedTask;
        }
    }

    private sealed class StubResolver : ISmtp2GoSourceIpResolver
    {
        public ValueTask<IReadOnlyCollection<IPAddress>> GetAddressesAsync(CancellationToken cancellationToken)
        {
            return new ValueTask<IReadOnlyCollection<IPAddress>>(Array.Empty<IPAddress>());
        }
    }

}

internal static class HandlerRegistration
{
    /// <summary>Registers a recording handler for <typeparamref name="TEvent"/> that appends <paramref name="name"/> to the container's <see cref="HandlerDispatchTests.Recorder"/>.</summary>
    public static void AddHandler<TEvent>(this IServiceCollection services, string name, bool throwing = false)
        where TEvent : WebhookEvent
    {
        services.AddSingleton<IWebhookEventHandler<TEvent>>(provider => new RecordingHandler<TEvent>(provider.GetRequiredService<HandlerDispatchTests.Recorder>(), name, throwing));
    }

    private sealed class RecordingHandler<TEvent>(HandlerDispatchTests.Recorder recorder, string name, bool throwing) : IWebhookEventHandler<TEvent>
        where TEvent : WebhookEvent
    {
        public Task HandleAsync(TEvent webhookEvent, CancellationToken cancellationToken)
        {
            recorder.Calls.Add(name);
            recorder.Events.Add(webhookEvent);
            recorder.Tokens.Add(cancellationToken);
            return throwing ? throw new InvalidOperationException(name + " failed") : Task.CompletedTask;
        }
    }
}
