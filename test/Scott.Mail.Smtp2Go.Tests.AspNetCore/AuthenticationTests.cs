using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Scott.Mail.Smtp2Go.Tests.AspNetCore;

public class AuthenticationTests
{
    private const string Fixture = "Docs/delivered.json";

    [Fact]
    public async Task Basic_with_the_configured_credentials_is_accepted()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(map: (test, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path, test.RecordAsync).RequireBasicAuth("hook", "s3cret"));

        using HttpResponseMessage response = await host.PostFixtureAsync(Fixture, request => request.Headers.Authorization = WebhookTestHost.Basic("hook", "s3cret"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        host.Received.Should().ContainSingle();
    }

    [Theory]
    [InlineData("hook", "wrong")]
    [InlineData("other", "s3cret")]
    [InlineData("hook", "s3cret ")]
    [InlineData("hook", "")]
    public async Task Basic_with_other_credentials_is_rejected_with_401_before_the_body_is_read(string user, string password)
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(map: (test, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path, test.RecordAsync).RequireBasicAuth("hook", "s3cret"));

        using HttpResponseMessage response = await host.PostAsync("not even json", "text/plain", request => request.Headers.Authorization = WebhookTestHost.Basic(user, password));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "authentication runs before the media type check");
        response.Headers.WwwAuthenticate.Should().ContainSingle().Which.ToString().Should().Be("Basic realm=\"smtp2go\"");
        host.Received.Should().BeEmpty();
        CapturingLoggerProvider.LogEntry entry = host.Logs.Smtp2Go.Should().ContainSingle(e => e.EventId.Id == Smtp2GoWebhookEventIds.Unauthorized).Subject;
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Message.Should().Contain("Basic").And.NotContain("s3cret").And.NotContain(Convert.ToBase64String(Encoding.UTF8.GetBytes(user + ":" + password)));
    }

    [Fact]
    public async Task Missing_header_is_rejected_with_401_and_a_challenge()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(map: (test, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path, test.RecordAsync).RequireBasicAuth("hook", "s3cret"));

        using HttpResponseMessage response = await host.PostFixtureAsync(Fixture);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.Should().ContainSingle().Which.Scheme.Should().Be("Basic");
        host.Logs.Smtp2Go.Should().ContainSingle(e => e.EventId.Id == Smtp2GoWebhookEventIds.Unauthorized).Which.Message.Should().Contain("none");
    }

    [Theory]
    [InlineData("Bearer s3cret")]
    [InlineData("Basic")]
    [InlineData("Basic not-base64!")]
    [InlineData("Basic aG9va3MzY3JldA==")]
    [InlineData("Digest username=\"hook\"")]
    public async Task Other_schemes_and_malformed_values_are_rejected_with_401(string header)
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(map: (test, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path, test.RecordAsync).RequireBasicAuth("hook", "s3cret"));

        using HttpResponseMessage response = await host.PostFixtureAsync(Fixture, request => request.Headers.TryAddWithoutValidation("Authorization", header));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        host.Received.Should().BeEmpty();
    }

    [Fact]
    public async Task Url_userinfo_credentials_arrive_as_basic_and_are_accepted_literal_or_percent_decoded()
    {
        // The webhook was registered as https://us%40er:p%3Ass%2F@host/webhooks/smtp2go; the application knows the real user name and password.
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(map: (test, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path, test.RecordAsync).RequireBasicAuth("us@er", "p:ss/"));

        using HttpResponseMessage decoded = await host.PostFixtureAsync(Fixture, request => request.Headers.Authorization = WebhookTestHost.Basic("us@er", "p:ss/"));
        using HttpResponseMessage literal = await host.PostFixtureAsync(Fixture, request => request.Headers.Authorization = WebhookTestHost.Basic("us%40er", "p%3Ass%2F"));
        using HttpResponseMessage wrong = await host.PostFixtureAsync(Fixture, request => request.Headers.Authorization = WebhookTestHost.Basic("us%40er", "p%3Ass"));

        decoded.StatusCode.Should().Be(HttpStatusCode.OK);
        literal.StatusCode.Should().Be(HttpStatusCode.OK);
        wrong.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        host.Received.Should().HaveCount(2);
    }

    [Fact]
    public async Task Bearer_with_the_configured_token_is_accepted_and_others_rejected()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(map: (test, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path, test.RecordAsync).RequireBearer("tok-123"));

        using HttpResponseMessage accepted = await host.PostFixtureAsync(Fixture, request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "tok-123"));
        using HttpResponseMessage wrongToken = await host.PostFixtureAsync(Fixture, request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "tok-124"));
        using HttpResponseMessage longer = await host.PostFixtureAsync(Fixture, request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "tok-1234"));
        using HttpResponseMessage basic = await host.PostFixtureAsync(Fixture, request => request.Headers.Authorization = WebhookTestHost.Basic("tok-123", "tok-123"));
        using HttpResponseMessage missing = await host.PostFixtureAsync(Fixture);

        accepted.StatusCode.Should().Be(HttpStatusCode.OK);
        wrongToken.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        longer.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        basic.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        missing.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        missing.Headers.WwwAuthenticate.Should().ContainSingle().Which.Scheme.Should().Be("Bearer");
        host.Received.Should().ContainSingle();
    }

    [Fact]
    public async Task Basic_and_bearer_together_accept_either()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(map: (test, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path, test.RecordAsync)
            .RequireBasicAuth("hook", "s3cret")
            .RequireBearer("tok-123"));

        using HttpResponseMessage basic = await host.PostFixtureAsync(Fixture, request => request.Headers.Authorization = WebhookTestHost.Basic("hook", "s3cret"));
        using HttpResponseMessage bearer = await host.PostFixtureAsync(Fixture, request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "tok-123"));
        using HttpResponseMessage neither = await host.PostFixtureAsync(Fixture, request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "s3cret"));

        basic.StatusCode.Should().Be(HttpStatusCode.OK);
        bearer.StatusCode.Should().Be(HttpStatusCode.OK);
        neither.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        neither.Headers.WwwAuthenticate.Select(challenge => challenge.Scheme).Should().BeEquivalentTo("Basic", "Bearer");
    }

    [Fact]
    public async Task Credentials_from_a_delegate_are_read_from_the_request_services_on_every_request()
    {
        SecretStore store = new() { Username = "hook", Password = "first", Token = "tok-1" };
        await using WebhookTestHost host = await WebhookTestHost.StartAsync(
            services => services.AddSingleton(store),
            (test, app) => app.MapSmtp2GoWebhook(WebhookTestHost.Path, test.RecordAsync)
                .RequireBasicAuth(provider => (provider.GetRequiredService<SecretStore>().Username, provider.GetRequiredService<SecretStore>().Password))
                .RequireBearer(provider => provider.GetRequiredService<SecretStore>().Token));

        using HttpResponseMessage before = await host.PostFixtureAsync(Fixture, request => request.Headers.Authorization = WebhookTestHost.Basic("hook", "first"));
        store.Password = "second";
        store.Token = "tok-2";
        using HttpResponseMessage stale = await host.PostFixtureAsync(Fixture, request => request.Headers.Authorization = WebhookTestHost.Basic("hook", "first"));
        using HttpResponseMessage rotated = await host.PostFixtureAsync(Fixture, request => request.Headers.Authorization = WebhookTestHost.Basic("hook", "second"));
        using HttpResponseMessage rotatedToken = await host.PostFixtureAsync(Fixture, request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "tok-2"));

        before.StatusCode.Should().Be(HttpStatusCode.OK);
        stale.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        rotated.StatusCode.Should().Be(HttpStatusCode.OK);
        rotatedToken.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Without_a_require_call_the_endpoint_is_open()
    {
        await using WebhookTestHost host = await WebhookTestHost.StartAsync();

        using HttpResponseMessage response = await host.PostFixtureAsync(Fixture, request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "anything"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("hook:s3cret", true)]
    [InlineData("hook:s3cret:extra", false)]
    [InlineData("hook:s3cre", false)]
    [InlineData("hook:s3cret!", false)]
    [InlineData("hooks3cret", false)]
    [InlineData("", false)]
    public void MatchesBasic_compares_user_and_password_split_at_the_first_colon(string pair, bool expected)
    {
        string parameter = Convert.ToBase64String(Encoding.UTF8.GetBytes(pair));

        WebhookAuthenticator.MatchesBasic(parameter, "hook", "s3cret").Should().Be(expected);
    }

    [Fact]
    public void MatchesBasic_allows_colons_in_the_password()
    {
        string parameter = Convert.ToBase64String(Encoding.UTF8.GetBytes("hook:a:b:c"));

        WebhookAuthenticator.MatchesBasic(parameter, "hook", "a:b:c").Should().BeTrue();
    }

    [Fact]
    public void Builder_rejects_empty_credentials()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        using WebApplication app = builder.Build();
        Smtp2GoWebhookEndpointConventionBuilder endpoint = app.MapSmtp2GoWebhook(WebhookTestHost.Path, (_, _) => Task.CompletedTask);

        ((Action)(() => endpoint.RequireBasicAuth("", "x"))).Should().Throw<ArgumentException>();
        ((Action)(() => endpoint.RequireBearer(""))).Should().Throw<ArgumentException>();
        ((Action)(() => endpoint.RequireBearer((Func<IServiceProvider, string>)null!))).Should().Throw<ArgumentNullException>();
    }

    private sealed class SecretStore
    {
        public string Username { get; set; } = "";

        public string Password { get; set; } = "";

        public string Token { get; set; } = "";
    }
}
