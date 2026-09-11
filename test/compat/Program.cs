using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Scott.Mail.Smtp2Go.Webhooks;

namespace Scott.Mail.Smtp2Go.Compat.Net48;

/// <summary>
/// Runs the packed core package on .NET Framework 4.8: a send round trip through a canned <see cref="HttpMessageHandler"/> (no network)
/// and a webhook callback parse. Exit code 0 means the netstandard2.0 asset loaded, its package dependencies resolved and both paths worked.
/// </summary>
internal static class Program
{
    private const string SendResponse = """
        {"request_id":"aa253464-0bd0-467a-b24b-6159dcd7be60","data":{"succeeded":1,"failed":0,"failures":[],"email_id":"1u0SwL-B9zBpi9ffUq-JAB2"}}
        """;

    private const string BounceCallback = """
        {"event":"bounce","time":"2026-09-10T08:00:07Z","sendtime":"2026-09-10T08:00:00Z","sender":"bounce@example.com","from":"Alice <alice@example.com>",
         "from_address":"alice@example.com","from_name":"Alice","rcpt":"nobody@example.org","auth":"api-5BFDE1E62529","host":"mx.example.org [198.51.100.25]",
         "message":"550 5.1.1 The email account that you tried to reach does not exist","context":"RCPT TO:<nobody@example.org>",
         "email_id":"1u0SwL-B9zBpi9ffUq-JAB3","id":"2d8e6e3f4a5b6c7d8e9f0a1b2c3d4e5f","message-id":"<E1u0SwL-B9zBpi9ffUq-JAB3@message-id.smtpcorp.com>",
         "bounce":"hard","subject":"Docs bounce"}
        """;

    private static async Task<int> Main()
    {
        try
        {
            using var handler = new CannedHandler(SendResponse);
            using var httpClient = new HttpClient(handler);
            var client = new Smtp2GoClient(httpClient, new Smtp2GoClientOptions { ApiKey = "api-" + new string('A', 32) });

            var response = await client.Email.SendAsync(new EmailSendRequest
            {
                Sender = "Alice <alice@example.com>",
                To = ["bob@example.com"],
                Subject = "Hello from net48",
                TextBody = "It works.",
            }).ConfigureAwait(false);

            if (response.Data.Succeeded != 1 || handler.ApiKeyHeader != "api-" + new string('A', 32))
            {
                Console.Error.WriteLine("Unexpected send result: succeeded={0}, header={1}", response.Data.Succeeded, handler.ApiKeyHeader);
                return 1;
            }

            WebhookEvent callback = WebhookPayloadParser.Default.Parse(BounceCallback);
            if (callback is not EmailBounceEvent)
            {
                Console.Error.WriteLine("Unexpected callback type: {0}", callback.GetType().FullName);
                return 1;
            }

            Console.WriteLine(
                "OK: Scott.Mail.Smtp2Go {0} on {1} ({2}); email_id {3}, callback {4}",
                typeof(Smtp2GoClient).Assembly.GetName().Version,
                Environment.Version,
                typeof(Smtp2GoClient).Assembly.Location,
                response.Data.EmailId,
                callback.GetType().Name);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    /// <summary>Answers every request with the canned body and records the API key header it received.</summary>
    private sealed class CannedHandler(string body) : HttpMessageHandler
    {
        public string? ApiKeyHeader { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.TryGetValues("X-Smtp2go-Api-Key", out var values))
            {
                ApiKeyHeader = string.Join(",", values);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }
}
