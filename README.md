# Scott.Mail.Smtp2Go

A .NET client for the whole [SMTP2GO](https://www.smtp2go.com/) v3 API: sending (JSON, MIME, batch, scheduled), webhooks, reporting and statistics, templates, suppressions, allowed senders and recipients, API keys, SMTP users, domains, subaccounts, email archiving and SMS. Targets `netstandard2.0`, `net8.0` and `net10.0`, is trim- and AOT-compatible, sends the API key in a header rather than the body, surfaces the API's own error payloads as typed exceptions, and ships opt-in packages for `Microsoft.Extensions.DependencyInjection` and ASP.NET Core webhook endpoints.

## Install

```shell
dotnet package add Scott.Mail.Smtp2Go
```

Optional: `Scott.Mail.Smtp2Go.DependencyInjection` (options, `IHttpClientFactory`, resilience) and `Scott.Mail.Smtp2Go.AspNetCore` (`MapSmtp2GoWebhook`).

## Quick start

```csharp
using Scott.Mail.Smtp2Go;

var client = new Smtp2GoClient(apiKey: "api-...");

var response = await client.Email.SendAsync(new EmailSendRequest
{
    Sender = "Alice <alice@example.com>",
    To = ["bob@example.com"],
    Subject = "Hello from Scott.Mail.Smtp2Go",
    TextBody = "It works.",
});

Console.WriteLine($"Sent: {response.Data.Succeeded} succeeded, {response.Data.Failed} failed ({response.RequestId})");
```

The quick start above is compiled by a unit test so it stays in sync with the API. See [docs/sending.md](docs/sending.md) for MIME and batch sends, `fastaccept`, scheduling, attachments, templates and `EnsureAccepted()`, and [docs/index.md](docs/index.md) for configuration, dependency injection, webhooks and API coverage.

## Status

Core transport is in place: construction, header authentication, regional endpoints, subaccount injection, client-side validation, the response envelope, typed errors, tracing and the `client.Raw` escape hatch that can call any endpoint today (see [docs/getting-started.md](docs/getting-started.md)). `client.Email` covers sending (JSON, MIME, batch), scheduled-email search and removal, and the deprecated email search. `client.Webhooks` manages webhooks and `Scott.Mail.Smtp2Go.Webhooks.WebhookPayloadParser` parses their JSON and form callbacks ([docs/webhooks.md](docs/webhooks.md)); `client.Stats`, `client.Activity`, `client.Templates` and `client.Suppressions` cover reporting, templates and suppressions ([docs/reporting.md](docs/reporting.md)); `client.Archive` searches the email archive and downloads originals ([docs/archive.md](docs/archive.md)). The full round trip, send then find in the archive then download the `.eml`, is the runnable [demo](demo/Scott.Mail.Smtp2Go.Demo/Program.cs). Account management and SMS follow in later plans.

## License

[LGPL-3.0-or-later](LICENSE)
