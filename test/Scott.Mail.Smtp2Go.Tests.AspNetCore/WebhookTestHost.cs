using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Webhooks;

namespace Scott.Mail.Smtp2Go.Tests.AspNetCore;

/// <summary>An in-memory ASP.NET Core application (TestServer) with one MapSmtp2GoWebhook endpoint, a capturing logger and helpers to post the callback fixtures.</summary>
internal sealed class WebhookTestHost : IAsyncDisposable
{
    public const string Path = "/webhooks/smtp2go";

    private readonly WebApplication _app;

    private WebhookTestHost(WebApplication app, CapturingLoggerProvider logs)
    {
        _app = app;
        Logs = logs;
    }

    public HttpClient Client { get; private set; } = null!;

    public CapturingLoggerProvider Logs { get; }

    public IServiceProvider Services => _app.Services;

    /// <summary>Every event <see cref="RecordAsync"/> received, in order.</summary>
    public List<WebhookEvent> Received { get; } = [];

    /// <summary>
    /// Starts an application. <paramref name="map"/> maps the endpoint (default: the inline delegate <see cref="RecordAsync"/>);
    /// <paramref name="remoteIp"/> is stamped on every connection before routing, as Kestrel or a correctly configured forwarded-headers middleware would.
    /// </summary>
    public static async Task<WebhookTestHost> StartAsync(
        Action<IServiceCollection>? services = null,
        Func<WebhookTestHost, WebApplication, Smtp2GoWebhookEndpointConventionBuilder>? map = null,
        IPAddress? remoteIp = null)
    {
        CapturingLoggerProvider logs = new();
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
        builder.Logging.AddProvider(logs);
        services?.Invoke(builder.Services);

        WebApplication app = builder.Build();
        if (remoteIp is not null)
        {
            app.Use((context, next) =>
            {
                context.Connection.RemoteIpAddress = remoteIp;
                return next(context);
            });
        }

        WebhookTestHost host = new(app, logs);
        if (map is null)
        {
            app.MapSmtp2GoWebhook(Path, host.RecordAsync);
        }
        else
        {
            map(host, app);
        }

        await app.StartAsync(TestContext.Current.CancellationToken);
        host.Client = app.GetTestClient();
        return host;
    }

    /// <summary>The default inline handler: records the event.</summary>
    public Task RecordAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        Received.Add(webhookEvent);
        return Task.CompletedTask;
    }

    /// <summary>Posts <paramref name="body"/> with <paramref name="contentType"/>; <see langword="null"/> sends no Content-Type header at all.</summary>
    public Task<HttpResponseMessage> PostAsync(string body, string? contentType, Action<HttpRequestMessage>? configure = null)
    {
        StringContent content = new(body, Encoding.UTF8, contentType ?? "text/plain");
        if (contentType is null)
        {
            content.Headers.ContentType = null;
        }

        return PostAsync(content, configure);
    }

    public async Task<HttpResponseMessage> PostAsync(HttpContent content, Action<HttpRequestMessage>? configure = null)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, Path) { Content = content };
        configure?.Invoke(request);
        return await Client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>Posts a fixture from <c>Fixtures/Webhooks/</c> with the media type its extension implies (<c>.json</c> or <c>.form</c>).</summary>
    public Task<HttpResponseMessage> PostFixtureAsync(string relativePath, Action<HttpRequestMessage>? configure = null)
    {
        bool json = relativePath.EndsWith(".json", StringComparison.Ordinal);
        return PostAsync(ReadFixture(relativePath), json ? "application/json" : "application/x-www-form-urlencoded", configure);
    }

    /// <summary>Reads a fixture from <c>Fixtures/Webhooks/</c> without the file's trailing newline (a real form body has none).</summary>
    public static string ReadFixture(string relativePath)
    {
        return Fixture.Read("Webhooks/" + relativePath).TrimEnd('\r', '\n');
    }

    /// <summary>Posts a <c>.form</c> fixture re-encoded as <c>multipart/form-data</c>.</summary>
    public Task<HttpResponseMessage> PostFixtureAsMultipartAsync(string relativePath)
    {
        MultipartFormDataContent content = [];
        foreach (string part in ReadFixture(relativePath).Split('&'))
        {
            int equals = part.IndexOf('=', StringComparison.Ordinal);
            string key = Uri.UnescapeDataString(part[..equals].Replace('+', ' '));
            string value = Uri.UnescapeDataString(part[(equals + 1)..].Replace('+', ' '));
            content.Add(new StringContent(value), key);
        }

        return PostAsync(content);
    }

    public static AuthenticationHeaderValue Basic(string user, string password)
    {
        return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(user + ":" + password)));
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.DisposeAsync();
    }
}
