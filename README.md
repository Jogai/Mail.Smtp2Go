# Scott.Mail.Smtp2Go

[![ci](https://flat.badgen.net/github/checks/Jogai/Mail.Smtp2Go/master?style=flat&label=ci)](https://github.com/Jogai/Mail.Smtp2Go/actions/workflows/ci.yml)
[![NuGet](https://flat.badgen.net/nuget/v/Scott.Mail.Smtp2Go?style=flat)](https://www.nuget.org/packages?q=Tags%3Asmtp2go+id%3AScott.Mail)
[![License: LGPL-3.0-or-later](https://flat.badgen.net/static/license/LGPL-3.0-or-later/blue?style=flat)](LICENSE)

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

The quick start above is compiled by a unit test so it stays in sync with the API.

## Documentation

Start with [Getting started](docs/getting-started.md), then pick what you need:

| Document | Covers |
| :-- | :-- |
| [Sending email](docs/sending.md) | JSON, MIME and batch sends, `fastaccept`, scheduling, attachments, templates, `EnsureAccepted()` |
| [Webhooks](docs/webhooks.md) | Managing webhooks, parsing callbacks, receiving them in ASP.NET Core |
| [Reporting](docs/reporting.md) | Statistics, quota, activity search and paging |
| [Archive](docs/archive.md) | Searching archived email and downloading the original message |
| [Account management and SMS](docs/account-management.md) | API keys, SMTP users, domains, senders, allowed lists, subaccounts, dedicated IPs, SMS |
| [Configuration](docs/configuration.md) | Client options, regions, authentication, dependency injection, resilience |
| [Observability](docs/observability.md) | Logging, metrics and tracing |
| [Errors](docs/errors.md) | The exception types and how API responses map to them |
| [API coverage](docs/api-coverage.md) | Every SMTP2GO endpoint and the client member that calls it |
| [API notes](docs/api-notes.md) | Where the live API and its documentation disagree |
| [Contributing](docs/contributing.md) | Building, testing and releasing |

Runnable examples are in [demo/](demo): a send, archive search and download [round trip](demo/Scott.Mail.Smtp2Go.Demo/Program.cs), a [generic host](demo/Scott.Mail.Smtp2Go.Demo.Hosting/Program.cs) with dependency injection, and a [webhook receiver](demo/Scott.Mail.Smtp2Go.Demo.WebhookReceiver/Program.cs). Release history is in the [changelog](CHANGELOG.md).

## Security

See [SECURITY.md](SECURITY.md) for how to report a vulnerability.

## License

[LGPL-3.0-or-later](LICENSE)
