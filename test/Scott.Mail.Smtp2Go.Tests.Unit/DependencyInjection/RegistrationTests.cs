using System.Net;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Scott.Mail.Smtp2Go.DependencyInjection;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.DependencyInjection;

public sealed class RegistrationTests
{
    private const string MarketingKey = "api-MARKETING0123456789ABCDEFGHIJKL";

    [Fact]
    public void Binds_every_option_from_configuration_including_nested_resilience()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Smtp2Go:ApiKey"] = TestClient.ApiKey,
            ["Smtp2Go:AuthenticationScheme"] = "Bearer",
            ["Smtp2Go:Region"] = "EU",
            ["Smtp2Go:BaseUrl"] = "https://proxy.example/v3/",
            ["Smtp2Go:Timeout"] = "00:00:42",
            ["Smtp2Go:DefaultFastAccept"] = "true",
            ["Smtp2Go:DefaultSubaccountId"] = "sub-1",
            ["Smtp2Go:ClientSideValidation"] = "false",
            ["Smtp2Go:Resilience:MaxRetries"] = "5",
            ["Smtp2Go:Resilience:RetryBaseDelay"] = "00:00:02",
            ["Smtp2Go:Resilience:RetryOnSendEndpoints"] = "true",
            ["Smtp2Go:Resilience:AttemptTimeout"] = "00:00:10",
            ["Smtp2Go:Resilience:TotalTimeout"] = "00:01:00",
            ["Smtp2Go:Resilience:CircuitBreaker:Enabled"] = "false",
            ["Smtp2Go:Resilience:CircuitBreaker:FailureRatio"] = "0.25",
            ["Smtp2Go:Resilience:CircuitBreaker:MinimumThroughput"] = "4",
            ["Smtp2Go:Resilience:CircuitBreaker:SamplingDuration"] = "00:00:05",
            ["Smtp2Go:Resilience:CircuitBreaker:BreakDuration"] = "00:00:07",
            ["Smtp2Go:Resilience:RateLimiting:Enabled"] = "true",
            ["Smtp2Go:Resilience:RateLimiting:GlobalConcurrency"] = "8",
            ["Smtp2Go:Resilience:RateLimiting:GlobalQueueLimit"] = "16",
            ["Smtp2Go:Resilience:RateLimiting:Overrides:ActivitySearch:PermitLimit"] = "120",
            ["Smtp2Go:Resilience:RateLimiting:Overrides:ActivitySearch:Window"] = "00:01:00",
            ["Smtp2Go:Resilience:RateLimiting:Overrides:ActivitySearch:QueueLimit"] = "5",
        });

        ServiceCollection services = new();
        services.AddSmtp2Go(configuration.GetSection("Smtp2Go"));
        using ServiceProvider provider = services.BuildServiceProvider();

        Smtp2GoOptions options = provider.GetRequiredService<IOptions<Smtp2GoOptions>>().Value;

        options.ApiKey.Should().Be(TestClient.ApiKey);
        options.AuthenticationScheme.Should().Be(AuthenticationScheme.Bearer);
        options.Region.Should().Be(Region.EU);
        options.BaseUrl.Should().Be(new Uri("https://proxy.example/v3/"));
        options.Timeout.Should().Be(TimeSpan.FromSeconds(42));
        options.DefaultFastAccept.Should().BeTrue();
        options.DefaultSubaccountId.Should().Be("sub-1");
        options.ClientSideValidation.Should().BeFalse();

        ResilienceOptions resilience = options.Resilience;
        resilience.MaxRetries.Should().Be(5);
        resilience.RetryBaseDelay.Should().Be(TimeSpan.FromSeconds(2));
        resilience.RetryOnSendEndpoints.Should().BeTrue();
        resilience.AttemptTimeout.Should().Be(TimeSpan.FromSeconds(10));
        resilience.TotalTimeout.Should().Be(TimeSpan.FromMinutes(1));
        resilience.CircuitBreaker.Enabled.Should().BeFalse();
        resilience.CircuitBreaker.FailureRatio.Should().Be(0.25);
        resilience.CircuitBreaker.MinimumThroughput.Should().Be(4);
        resilience.CircuitBreaker.SamplingDuration.Should().Be(TimeSpan.FromSeconds(5));
        resilience.CircuitBreaker.BreakDuration.Should().Be(TimeSpan.FromSeconds(7));
        resilience.RateLimiting.Enabled.Should().BeTrue();
        resilience.RateLimiting.GlobalConcurrency.Should().Be(8);
        resilience.RateLimiting.GlobalQueueLimit.Should().Be(16);
        resilience.RateLimiting.Overrides.Should().ContainKey(RateLimitClass.ActivitySearch);
        RateLimitWindow window = resilience.RateLimiting.Overrides[RateLimitClass.ActivitySearch];
        window.PermitLimit.Should().Be(120);
        window.Window.Should().Be(TimeSpan.FromMinutes(1));
        window.QueueLimit.Should().Be(5);
    }

    [Fact]
    public void Validation_failure_on_start_names_the_configuration_path()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Smtp2Go:Marketing:Resilience:CircuitBreaker:FailureRatio"] = "2",
            ["Smtp2Go:Marketing:Resilience:RateLimiting:Overrides:EmailSearch:PermitLimit"] = "0",
        });

        ServiceCollection services = new();
        services.AddSmtp2Go("marketing", configuration.GetSection("Smtp2Go:Marketing"));
        using ServiceProvider provider = services.BuildServiceProvider();

        Action act = () => provider.GetRequiredService<IStartupValidator>().Validate();

        act.Should().Throw<OptionsValidationException>().Which.Failures.Should().Contain(
        [
            "Smtp2Go:Marketing:ApiKey is required.",
            "Smtp2Go:Marketing:Resilience:CircuitBreaker:FailureRatio must be greater than 0 and at most 1.",
            "Smtp2Go:Marketing:Resilience:RateLimiting:Overrides:EmailSearch:PermitLimit must be at least 1.",
            "Smtp2Go:Marketing:Resilience:RateLimiting:Overrides:EmailSearch:Window must be a positive duration.",
        ]);
    }

    [Fact]
    public void Validation_of_code_configured_options_uses_the_default_path()
    {
        ServiceCollection services = new();
        services.AddSmtp2Go(options => options.Resilience.AttemptTimeout = TimeSpan.Zero);
        using ServiceProvider provider = services.BuildServiceProvider();

        Action act = () => provider.GetRequiredService<IOptions<Smtp2GoOptions>>().Value.ToString();

        act.Should().Throw<OptionsValidationException>().Which.Failures.Should().Contain(
        [
            "Smtp2Go:ApiKey is required.",
            "Smtp2Go:Resilience:AttemptTimeout must be a positive duration.",
        ]);
    }

    [Fact]
    public void Valid_options_pass_start_up_validation()
    {
        ServiceCollection services = new();
        services.AddSmtp2Go(options => options.ApiKey = TestClient.ApiKey);
        services.AddSmtp2Go("marketing", options =>
        {
            options.ApiKey = MarketingKey;
            options.Resilience.RateLimiting.Overrides[RateLimitClass.SubaccountAdd] = new RateLimitWindow { PermitLimit = 100, Window = TimeSpan.FromHours(1) };
        });
        using ServiceProvider provider = services.BuildServiceProvider();

        Action act = () => provider.GetRequiredService<IStartupValidator>().Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public async Task Named_clients_resolve_with_distinct_keys_and_base_urls()
    {
        FakeHttpMessageHandler defaultHandler = new();
        FakeHttpMessageHandler marketingHandler = new();
        ServiceCollection services = new();
        services.AddSmtp2Go(options =>
        {
            options.ApiKey = TestClient.ApiKey;
            options.Region = Region.US;
        }).HttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => defaultHandler);
        services.AddSmtp2Go("marketing", options =>
        {
            options.ApiKey = MarketingKey;
            options.Region = Region.EU;
        }).HttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => marketingHandler);
        using ServiceProvider provider = services.BuildServiceProvider();

        ISmtp2GoClientFactory factory = provider.GetRequiredService<ISmtp2GoClientFactory>();
        ISmtp2GoClient unnamed = provider.GetRequiredService<ISmtp2GoClient>();
        ISmtp2GoClient marketing = factory.Create("marketing");
        ISmtp2GoClient keyed = provider.GetRequiredKeyedService<ISmtp2GoClient>("marketing");

        unnamed.Should().BeOfType<Smtp2GoClient>().Which.Options.Region.Should().Be(Region.US);
        marketing.Should().BeOfType<Smtp2GoClient>().Which.Options.Region.Should().Be(Region.EU);
        keyed.Should().BeOfType<Smtp2GoClient>().Which.Options.ApiKey.Should().Be(MarketingKey);

        using (await unnamed.Raw.SendJsonAsync("stats/email_cycle", default, cancellationToken: TestContext.Current.CancellationToken))
        {
        }

        using (await keyed.Raw.SendJsonAsync("stats/email_cycle", default, cancellationToken: TestContext.Current.CancellationToken))
        {
        }

        defaultHandler.LastRequest.Uri.Host.Should().Be("us-api.smtp2go.com");
        defaultHandler.LastRequest.Header("X-Smtp2go-Api-Key").Should().Be(TestClient.ApiKey);
        marketingHandler.LastRequest.Uri.Host.Should().Be("eu-api.smtp2go.com");
        marketingHandler.LastRequest.Header("X-Smtp2go-Api-Key").Should().Be(MarketingKey);
    }

    [Fact]
    public void A_named_registration_alone_does_not_register_the_unnamed_client()
    {
        ServiceCollection services = new();
        services.AddSmtp2Go("marketing", options => options.ApiKey = MarketingKey);
        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<ISmtp2GoClient>().Should().BeNull();
        provider.GetKeyedService<ISmtp2GoClient>("marketing").Should().NotBeNull();
        provider.GetRequiredService<ISmtp2GoClientFactory>().Create("marketing").Should().NotBeNull();
    }

    [Fact]
    public void Http_client_leaves_timeouts_to_the_pipeline_and_prefers_http2()
    {
        ServiceCollection services = new();
        ISmtp2GoBuilder builder = services.AddSmtp2Go(options => options.ApiKey = TestClient.ApiKey);
        ISmtp2GoBuilder named = services.AddSmtp2Go("marketing", options => options.ApiKey = MarketingKey);
        using ServiceProvider provider = services.BuildServiceProvider();

        builder.Name.Should().Be(Options.DefaultName);
        builder.Services.Should().BeSameAs(services);
        builder.HttpClientBuilder.Name.Should().Be("Scott.Mail.Smtp2Go");
        named.HttpClientBuilder.Name.Should().Be("Scott.Mail.Smtp2Go:marketing");

        using HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(builder.HttpClientBuilder.Name);
        client.Timeout.Should().Be(Timeout.InfiniteTimeSpan);
        client.DefaultRequestVersion.Should().Be(HttpVersion.Version20);
        client.DefaultVersionPolicy.Should().Be(HttpVersionPolicy.RequestVersionOrLower);
        client.BaseAddress.Should().BeNull();
    }

    [Fact]
    public async Task Builder_exposes_the_http_client_builder_for_extra_handlers()
    {
        FakeHttpMessageHandler handler = new();
        ServiceCollection services = new();
        ISmtp2GoBuilder builder = services.AddSmtp2Go(options => options.ApiKey = TestClient.ApiKey);
        builder.HttpClientBuilder
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddHttpMessageHandler(() => new StampingHandler());
        using ServiceProvider provider = services.BuildServiceProvider();

        using System.Text.Json.JsonDocument document = await provider.GetRequiredService<ISmtp2GoClient>().Raw.SendJsonAsync("stats/email_cycle", default, cancellationToken: TestContext.Current.CancellationToken);

        handler.LastRequest.Header("X-Stamp").Should().Be("stamped");
    }

    [Fact]
    public void Smtp2GoOptions_mirrors_every_core_client_option()
    {
        PropertyInfo[] core = typeof(Smtp2GoClientOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo property in core)
        {
            PropertyInfo? mirror = typeof(Smtp2GoOptions).GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance);
            mirror.Should().NotBeNull($"Smtp2GoOptions must expose {property.Name} under the same name so one configuration section binds both");
            mirror!.PropertyType.Should().Be(property.PropertyType, $"{property.Name} must have the core type");
            mirror.CanWrite.Should().Be(property.CanWrite, $"{property.Name} must be settable like the core property");
        }
    }

    [Fact]
    public void ToClientOptions_copies_every_property()
    {
        Smtp2GoOptions options = new()
        {
            ApiKey = TestClient.ApiKey,
            AuthenticationScheme = AuthenticationScheme.Bearer,
            Region = Region.AU,
            BaseUrl = new Uri("https://proxy.example/"),
            Timeout = TimeSpan.FromSeconds(9),
            DefaultFastAccept = false,
            DefaultSubaccountId = "sub",
            ClientSideValidation = false,
            AdditionalJsonTypeInfoResolver = ProbeJsonContext.Default,
        };

        Smtp2GoClientOptions client = options.ToClientOptions();

        client.Should().BeEquivalentTo(options, o => o.Excluding(x => x.Resilience));
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private sealed class StampingHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.TryAddWithoutValidation("X-Stamp", "stamped");
            return base.SendAsync(request, cancellationToken);
        }
    }
}
