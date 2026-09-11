using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Scott.Mail.Smtp2Go.Webhooks;

namespace Scott.Mail.Smtp2Go.Tests.Integration.Live;

/// <summary>
/// Receives real callbacks through a <c>cloudflared</c> quick tunnel: registers two webhooks (JSON and form output) pointing at a local listener, sends an email, waits for
/// processed and delivered callbacks (bounce, open and click too when the recipient produces them within the window), asserts each parses, and with
/// <c>SMTP2GO_CAPTURE=1</c> writes the raw bodies into <c>Fixtures/Webhooks/Live/</c> as <c>&lt;event&gt;.json</c> / <c>&lt;event&gt;.form</c>. Needs the live settings and
/// <c>cloudflared</c> on the PATH; skipped otherwise. Manual: <c>dotnet test --project test/Scott.Mail.Smtp2Go.Tests.Integration -- --filter-trait Category=Live</c>.
/// </summary>
[Trait("Category", "Live")]
public class LiveWebhookCaptureTests
{
    private static readonly TimeSpan s_callbackWindow = TimeSpan.FromMinutes(3);

    [Fact]
    public async Task Callbacks_arrive_in_both_formats_and_parse()
    {
        Smtp2GoClient client = LiveEnvironment.CreateClientOrSkip();
        Assert.SkipUnless(IsOnPath("cloudflared"), "cloudflared is not on the PATH; install it to run the webhook capture test.");
        CancellationToken ct = TestContext.Current.CancellationToken;

        int port = FreePort();
        using HttpListener listener = new();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        List<(string Path, string ContentType, string Body)> received = [];
        Task receiving = ReceiveAsync(listener, received, ct);

        using Process tunnel = StartTunnel(port);
        string publicUrl = await WaitForTunnelUrlAsync(tunnel, ct);
        List<long> webhookIds = [];
        try
        {
            foreach ((string path, WebhookOutputFormat format) in new[] { ("json", WebhookOutputFormat.Json), ("form", WebhookOutputFormat.Form) })
            {
                ApiResponse<Webhook> added = await client.Webhooks.AddOrUpdateByUrlAsync(new WebhookAddRequest
                {
                    Url = $"{publicUrl}/{path}",
                    Events = [WebhookEmailEvent.Processed, WebhookEmailEvent.Delivered, WebhookEmailEvent.Bounce, WebhookEmailEvent.Open, WebhookEmailEvent.Click, WebhookEmailEvent.Spam, WebhookEmailEvent.Unsubscribe, WebhookEmailEvent.Reject],
                    OutputFormat = format,
                }, cancellationToken: ct);
                webhookIds.Add(added.Data.Id ?? throw new InvalidOperationException("webhook/add returned no id."));
            }

            string marker = Guid.NewGuid().ToString("N");
            ApiResponse<EmailSendResult> sent = await client.Email.SendAsync(new EmailSendRequest
            {
                Sender = LiveEnvironment.Sender!,
                To = [LiveEnvironment.Recipient!],
                Subject = "Scott.Mail.Smtp2Go webhook capture " + marker,
                HtmlBody = $"<p>Webhook capture {marker}. <a href=\"https://www.smtp2go.com/\">A tracked link</a>.</p>",
                TextBody = "Webhook capture " + marker,
            }, cancellationToken: ct);
            string emailId = sent.EnsureAccepted().EmailId ?? throw new InvalidOperationException("No email_id.");

            DateTimeOffset deadline = DateTimeOffset.UtcNow + s_callbackWindow;
            while (DateTimeOffset.UtcNow < deadline && !(Has(received, "json", "delivered", emailId) && Has(received, "form", "delivered", emailId)))
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }

            List<(string Path, string ContentType, string Body)> ours = received.Where(r => r.Body.Contains(emailId, StringComparison.Ordinal)).ToList();
            ours.Should().NotBeEmpty(because: "at least the processed callback should arrive within {0}", s_callbackWindow);
            WebhookPayloadParser parser = new();
            foreach ((string path, string contentType, string body) in ours)
            {
                WebhookEvent evt = parser.Parse(body, contentType);
                evt.Kind.Should().NotBe(WebhookEventKind.Unknown, because: "{0} callback {1} must map to a known kind", path, evt.EventRaw);
                evt.Should().BeAssignableTo<EmailWebhookEvent>().Which.EmailId.Should().Be(emailId);
                if (LiveEnvironment.Capture)
                {
                    string file = Path.Combine(LiveEnvironment.LiveFixtureDirectory(), evt.EventRaw + (path == "json" ? ".json" : ".form"));
                    await File.WriteAllTextAsync(file, body.TrimEnd() + Environment.NewLine, ct);
                }
            }

            ours.Select(r => r.Path).Distinct().Should().BeEquivalentTo(["json", "form"], because: "both output formats deliver");
        }
        finally
        {
            foreach (long id in webhookIds)
            {
                await client.Webhooks.RemoveAsync(id, cancellationToken: CancellationToken.None);
            }

            listener.Stop();
            try
            {
                tunnel.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            await Task.WhenAny(receiving, Task.Delay(TimeSpan.FromSeconds(2), CancellationToken.None));
        }
    }

    private static bool Has(List<(string Path, string ContentType, string Body)> received, string path, string eventName, string emailId)
    {
        lock (received)
        {
            return received.Any(r => r.Path == path && r.Body.Contains(emailId, StringComparison.Ordinal) && r.Body.Contains(eventName, StringComparison.Ordinal));
        }
    }

    private static async Task ReceiveAsync(HttpListener listener, List<(string Path, string ContentType, string Body)> received, CancellationToken ct)
    {
        while (listener.IsListening && !ct.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync();
            }
            catch (HttpListenerException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            using StreamReader reader = new(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8);
            string body = await reader.ReadToEndAsync(ct);
            lock (received)
            {
                received.Add((context.Request.Url!.AbsolutePath.Trim('/'), context.Request.ContentType ?? string.Empty, body));
            }

            context.Response.StatusCode = 200;
            context.Response.Close();
        }
    }

    private static Process StartTunnel(int port)
    {
        ProcessStartInfo info = new("cloudflared", $"tunnel --url http://localhost:{port} --no-autoupdate")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        return Process.Start(info) ?? throw new InvalidOperationException("cloudflared did not start.");
    }

    private static async Task<string> WaitForTunnelUrlAsync(Process tunnel, CancellationToken ct)
    {
        Regex url = new(@"https://[a-z0-9-]+\.trycloudflare\.com", RegexOptions.IgnoreCase);
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(60));
        while (!tunnel.HasExited)
        {
            string? line = await tunnel.StandardError.ReadLineAsync(timeout.Token);
            if (line is null)
            {
                break;
            }

            Match match = url.Match(line);
            if (match.Success)
            {
                return match.Value;
            }
        }

        throw new InvalidOperationException("cloudflared did not print a trycloudflare.com URL.");
    }

    private static int FreePort()
    {
        using System.Net.Sockets.TcpListener probe = new(IPAddress.Loopback, 0);
        probe.Start();
        return ((IPEndPoint)probe.LocalEndpoint).Port;
    }

    private static bool IsOnPath(string executable)
    {
        string? path = Environment.GetEnvironmentVariable("PATH");
        if (path is null)
        {
            return false;
        }

        foreach (string directory in path.Split(Path.PathSeparator))
        {
            if (File.Exists(Path.Combine(directory, executable)) || File.Exists(Path.Combine(directory, executable + ".exe")))
            {
                return true;
            }
        }

        return false;
    }
}
