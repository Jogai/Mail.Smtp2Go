# Scott.Mail.Smtp2Go

A .NET client for the whole [SMTP2GO](https://www.smtp2go.com/) v3 API: sending (JSON, MIME, batch, scheduled), webhooks, reporting and statistics, templates, suppressions, allowed senders and recipients, API keys, SMTP users, domains, subaccounts, email archiving and SMS. Targets `netstandard2.0`, `net8.0` and `net10.0`, is trim- and AOT-compatible, sends the API key in a header rather than the body, and surfaces the API's own error payloads as typed exceptions.

## Install

```shell
dotnet package add Scott.Mail.Smtp2Go
```

| Package | Adds |
| :-- | :-- |
| `Scott.Mail.Smtp2Go` | The client: every documented operation, the webhook callback parser, typed errors, diagnostics hooks. |
| `Scott.Mail.Smtp2Go.DependencyInjection` | `AddSmtp2Go`: options binding and validation, `IHttpClientFactory`, resilience pipeline, named clients, logging and metrics. |
| `Scott.Mail.Smtp2Go.AspNetCore` | `MapSmtp2GoWebhook`: an endpoint that authenticates, checks and parses SMTP2GO callbacks and dispatches typed events to your handlers. |

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

## Links

- [Documentation](https://github.com/Jogai/Mail.Smtp2Go/blob/master/docs/index.md): getting started, sending, configuration, observability, errors, webhooks, reporting, archive, account management and SMS.
- [API coverage](https://github.com/Jogai/Mail.Smtp2Go/blob/master/docs/api-coverage.md): every SMTP2GO endpoint and the client member that covers it.
- [Changelog](https://github.com/Jogai/Mail.Smtp2Go/blob/master/CHANGELOG.md), [security policy](https://github.com/Jogai/Mail.Smtp2Go/blob/master/SECURITY.md), [source and issues](https://github.com/Jogai/Mail.Smtp2Go).

Licensed under [LGPL-3.0-or-later](https://github.com/Jogai/Mail.Smtp2Go/blob/master/LICENSE).
