# Scott.Mail.Smtp2Go

[![NuGet](https://img.shields.io/nuget/v/Scott.Mail.Smtp2Go?style=for-the-badge)](https://www.nuget.org/packages/Scott.Mail.Smtp2Go)
[![Target frameworks](https://img.shields.io/badge/.NET-8%20%7C%2010%20%7C%20netstandard2.0-purple?style=for-the-badge)](https://www.nuget.org/packages/Scott.Mail.Smtp2Go#supportedframeworks-body-tab)
[![API coverage](https://img.shields.io/badge/SMTP2GO%20API-69%20of%2069%20endpoints-green?style=for-the-badge)](docs/api-coverage.md)
[![Dependabot](https://img.shields.io/badge/dependabot-enabled-blue?style=for-the-badge&logo=dependabot)](.github/dependabot.yml)
[![Downloads](https://img.shields.io/nuget/dt/Scott.Mail.Smtp2Go?style=for-the-badge)](https://www.nuget.org/packages?q=Tags%3Asmtp2go+id%3AScott.Mail)
[![ci](https://img.shields.io/github/actions/workflow/status/Jogai/Mail.Smtp2Go/ci.yml?branch=master&style=for-the-badge&label=ci)](https://github.com/Jogai/Mail.Smtp2Go/actions/workflows/ci.yml)
[![License: LGPL-3.0-or-later](https://img.shields.io/badge/license-LGPL--3.0--or--later-blue?style=for-the-badge)](LICENSE)

A .NET client for the whole [SMTP2GO](https://www.smtp2go.com/) v3 API: sending (JSON, MIME, batch, scheduled), webhooks, reporting and statistics, templates, suppressions, allowed senders and recipients, API keys, SMTP users, domains, subaccounts, email archiving and SMS. Targets `netstandard2.0`, `net8.0` and `net10.0`, is trim- and AOT-compatible, sends the API key in a header rather than the body, surfaces the API's own error payloads as typed exceptions, and ships opt-in packages for `Microsoft.Extensions.DependencyInjection` and ASP.NET Core webhook endpoints.

## Features

- **The whole v3 API, typed.** 69 of 69 documented operations across 18 families, each with request and response records, reachable as `client.Email`, `client.Webhooks`, `client.Stats`, `client.Activity`, `client.Templates`, `client.Suppressions`, `client.Archive`, `client.ApiKeys`, `client.SmtpUsers`, `client.IpAuth`, `client.Domains`, `client.SingleSenders`, `client.AllowedSenders`, `client.AllowedRecipients`, `client.Subaccounts`, `client.DedicatedIps` and `client.Sms`. `client.Raw` reaches anything the API adds before the library does, with the same authentication, routing, validation and error mapping.
- **Current sending features.** `fastaccept`, `schedule` with scheduled-email search and removal, `email/batch`, URL-sourced attachments, templates without a subject, `EnsureAccepted()` to turn a 200 with failed recipients into an exception.
- **Authentication and routing.** The key goes in the `X-Smtp2go-Api-Key` header or as `Authorization: Bearer`; never in the body. `Region` (`US`, `EU`, `AU`) selects the regional endpoint, `SubaccountId` is merged into every call of an endpoint that documents it, and both can be overridden per call.
- **Errors you can act on.** `Smtp2GoApiException` carries the API's `error_code`, `error`, `request_id` and `field_validation_errors`; `Smtp2GoRateLimitException` parses `Retry-After`. Documented limits (50 MB under `email/*`, 1 MB elsewhere, 100 recipients per field) fail client-side before a byte is sent.
- **Resilience that matches the docs.** Retry only on idempotent endpoints (or when a call opts in), per-endpoint rate-limit classes for the limits SMTP2GO documents (60/min `activity/search`, 5/min `api_keys/add`, 50/hour `subaccount/add`), circuit breaker and timeouts through `Microsoft.Extensions.Http.Resilience`.
- **Webhooks end to end.** Managing webhooks, parsing JSON and form-encoded callbacks into typed events (unknown fields land in `Extra`, custom headers in `CustomHeaders`), and `MapSmtp2GoWebhook` for ASP.NET Core with bearer or basic authentication, a source-IP allow-list and `IWebhookEventHandler<TEvent>` dispatch from the concrete event up to `WebhookEvent`.
- **Paging and the archive.** `IAsyncEnumerable<T>` pagers over activity, templates, suppressions, subaccounts and the archive; downloading the original archived message.
- **Observability.** An `ActivitySource` span per call, a `Meter` with request, duration and email-result instruments, and source-generated logging with stable event ids.
- **Built to last.** `netstandard2.0`, `net8.0` and `net10.0`; trim- and AOT-compatible with source-generated JSON; the public API tracked with `PublicAPI.*.txt` and package validation against the previous release; the API reference harvested weekly into `docs/api-spec/`, with contract tests that fail when the models and the documentation disagree.

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
