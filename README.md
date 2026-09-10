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

The quick start above is compiled by a unit test so it stays in sync with the API (plan 03). See [docs/index.md](docs/index.md) for configuration, dependency injection, webhooks and API coverage.

## Status

Scaffold only. The client surface lands in the following plans; nothing in this package is usable yet.

## License

[LGPL-3.0-or-later](LICENSE)
