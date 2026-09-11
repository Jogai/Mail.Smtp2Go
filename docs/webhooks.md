# Webhooks

Three parts: managing webhooks through the API (`client.Webhooks`), parsing the callbacks SMTP2GO posts to your endpoint (`Scott.Mail.Smtp2Go.Webhooks.WebhookPayloadParser`), and [receiving them in ASP.NET Core](#receiving-callbacks) with `MapSmtp2GoWebhook` from the `Scott.Mail.Smtp2Go.AspNetCore` package, which builds on the parser.

## Management

`client.Webhooks` covers `webhook/view`, `webhook/add`, `webhook/edit` and `webhook/remove`, all of which accept `subaccount_id` through `RequestOptions.SubaccountId` or `Smtp2GoClientOptions.DefaultSubaccountId`. Every operation returns the full `Webhook` object (`Id`, `Url`, `Events`, `SmsEvents`, `Headers`, `Usernames`, `OutputFormat`, `AuthHeaderType`, `AuthHeaderValue`, `Extra`).

```csharp
using Scott.Mail.Smtp2Go;

var client = new Smtp2GoClient("api-...");

ApiResponse<Webhook> added = await client.Webhooks.AddAsync(new WebhookAddRequest
{
    Url = "https://app.example.com/webhooks/smtp2go",
    Events = [WebhookEmailEvent.Processed, WebhookEmailEvent.Delivered, WebhookEmailEvent.Bounce, WebhookEmailEvent.Open, WebhookEmailEvent.Click],
    Headers = ["X-Customer-Id"],           // custom email headers to include in every callback
    OutputFormat = WebhookOutputFormat.Json,
    AuthHeaderType = WebhookAuthHeaderType.Bearer,
    AuthHeaderValue = "a-long-random-token",
});
long id = added.Data.Id!.Value;

IReadOnlyList<Webhook> all = (await client.Webhooks.ViewAsync()).Data;
await client.Webhooks.EditAsync(new WebhookEditRequest { Id = id, Events = [WebhookEmailEvent.Bounce] });
Webhook removed = (await client.Webhooks.RemoveAsync(id)).Data;
```

`AddOrUpdateByUrlAsync(request)` lists the webhooks, edits the one whose `Url` matches and adds one otherwise, so a deployment can register its endpoint idempotently and the webhook id stays stable. Free plans allow one webhook, paid plans ten.

`WebhookEmailEvent` has the eight values the `webhook/add` reference lists plus `Resubscribe`, and `WebhookSmsEvent` the five documented plus `OptOut`; both extras come from the event tables on the Webhooks Overview page rather than the endpoint reference. Requests refuse `Unknown` members through client-side validation.

### Choose JSON output

The server default `output_format` is `form` (`application/x-www-form-urlencoded`). Set `OutputFormat = WebhookOutputFormat.Json`: arrays such as `recipients` arrive as arrays, header values keep their case and characters, and there is no ambiguity about repeated keys. The parser handles both, but JSON is the format to pick for new webhooks.

### Authentication: header versus URL credentials

SMTP2GO signs nothing. The documented options are credentials in the URL (`https://user:pass@host/path`, or a secret path or query string) and, newer, an `Authorization` header set through `AuthHeaderType` and `AuthHeaderValue`: `Basic` with `base64(user:pass)` or `Bearer` with a token. Prefer the header: URL credentials show up in logs and link previews. To clear the header on an existing webhook, edit with `AuthHeaderType = WebhookAuthHeaderType.None`, which sends the documented empty string. A second layer is allow-listing the A records of `webhooks.smtp2go.com`, which the AspNetCore package automates.

## Callbacks

`WebhookPayloadParser` turns a callback body into the `WebhookEvent` subtype for its `event` field:

```csharp
using Scott.Mail.Smtp2Go.Webhooks;

var parser = new WebhookPayloadParser(new WebhookParserOptions { KnownCustomHeaders = ["X-Customer-Id"] });

// JSON: a string, a UTF-8 span, or a stream (request body)
WebhookEvent evt = await parser.ParseAsync(request.Body);
// Form: pairs (for example from IFormCollection) or the raw urlencoded body
WebhookEvent fromForm = parser.ParseFormBody("event=delivered&rcpt=bob%40example.org");
// Or dispatch on the Content-Type header
WebhookEvent any = parser.Parse(body, request.ContentType);

switch (evt)
{
    case EmailDeliveredEvent delivered:
        Console.WriteLine($"{delivered.EmailId} delivered to {delivered.Recipient}: {delivered.Message}");
        break;
    case EmailBounceEvent bounce when bounce.BounceType == BounceType.Hard:
        Console.WriteLine($"hard bounce for {bounce.Recipient}: {bounce.Context}");
        break;
    case EmailClickEvent click:
        Console.WriteLine($"{click.Recipient} clicked {click.ClickUrl} from {click.GeoCountry}");
        break;
    case UnknownWebhookEvent unknown:
        Console.WriteLine($"unknown event {unknown.EventRaw}: {string.Join(",", unknown.Extra!.Keys)}");
        break;
}
```

### Types

| Type | `Kind` | Fields beyond the shared ones |
| :-- | :-- | :-- |
| `EmailProcessedEvent` | `EmailProcessed` | `Recipients` (all recipients) rather than `Recipient` |
| `EmailDeliveredEvent` | `EmailDelivered` | `Host`, `Message` (the 250 response), `Context` |
| `EmailBounceEvent` | `EmailBounce` | `BounceType` (`Hard`/`Soft`/`Unknown`), `BounceRaw`, `Host`, `Message`, `Context` |
| `EmailOpenEvent` | `EmailOpen` | `UserAgent`, `ReadSeconds`, `Client`, `ClientDevice`, `ClientOs`, `GeoContinent`, `GeoCountry`, `GeoCity` |
| `EmailClickEvent` (derives from `EmailOpenEvent`) | `EmailClick` | the open fields plus `Link`, `ClickUrl` |
| `EmailSpamEvent`, `EmailUnsubscribeEvent`, `EmailResubscribeEvent`, `EmailRejectEvent` | matching | none |
| `SmsStatusEvent` | `SmsSending`, `SmsSubmitted`, `SmsDelivered`, `SmsFailed`, `SmsRejected`, `SmsOptOut` | `DestinationNumber`, `EmailSubject`, `MessageContent`, `MessageId`, `ReceivedTimestamp`, `Region`, `RetryCount`, `SenderEmail`, `SourceNumber`, `StatusCode`, `SubmittedTimestamp` |
| `UnknownWebhookEvent` | `Unknown` | everything except `id` and `time` is in `Extra` |

Every email event (`EmailWebhookEvent`) carries `EmailId`, `MessageId`, `Subject`, `Sender`, `From`, `FromAddress`, `FromName`, `Recipient` (`rcpt`), `Recipients`, `SendTime`, `Auth`, `SourceHost` (`srchost`), `Host`, `Message`, `Context` and `CustomHeaders`. Every event carries `Kind`, `EventRaw`, `WebhookId` (`id`), `Time` and `Extra`.

### Docs names versus live names

The documentation says callbacks carry `open`, `click`, `spam`, `unsubscribe`, `resubscribe` and `reject`; the live API has been observed sending `opened`, `clicked`, `spam_complaint` and `unsubscribed` (and `Message-Id`/`Subject` keys in mixed case alongside their lower-case twins in form output). The parser accepts both spellings, resolves them to the same `Kind`, and keeps the exact wire string in `EventRaw`. Its fixtures come in two sets, `Fixtures/Webhooks/Docs` (from the documentation tables) and `Fixtures/Webhooks/Live` (re-created captures), and both must parse; see [api-notes](api-notes.md) for the full list of discrepancies.

### Custom headers and unknown fields

Headers requested through the webhook's `headers` setting arrive as extra top-level keys. Declare their names in `WebhookParserOptions.KnownCustomHeaders` and they land in `EmailWebhookEvent.CustomHeaders` under the declared name (matched case-insensitively, `-` and `_` equivalent); anything else the type does not model stays in `Extra` as `JsonElement`s keyed by the original wire name, so a new server field is never lost. Key matching everywhere is case-insensitive with `-` and `_` treated alike, which is what makes `message-id`, `Message-Id` and `message_id` one field.

Form specifics: repeated keys accumulate; `recipients` is split on `,` and `;` (in JSON too when it arrives as one string); timestamps that fail to parse become `null` rather than throwing.

### Operational notes from the docs

SMTP2GO waits 10 seconds for response headers, then retries up to 35 times over 48 hours (5 in the first 30 minutes, hourly for a day, then every 6 and 12 hours). Return 200 as soon as the body is stored; do the work afterwards. Failed notifications are listed under Settings > Webhooks > Failed Notifications in the app.

## Receiving callbacks

The `Scott.Mail.Smtp2Go.AspNetCore` package (net8.0 and net10.0; depends only on the core package and the ASP.NET Core shared framework) turns the parser into an endpoint:

```shell
dotnet package add Scott.Mail.Smtp2Go.AspNetCore
```

```csharp
using Scott.Mail.Smtp2Go.AspNetCore;
using Scott.Mail.Smtp2Go.Webhooks;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSmtp2GoWebhooks(options => options.KnownCustomHeaders.Add("X-Customer-Id"));
builder.Services.AddScoped<IWebhookEventHandler<EmailBounceEvent>, BounceHandler>();   // one per type you care about
builder.Services.AddSingleton<IWebhookEventHandler<WebhookEvent>, AuditHandler>();       // catch-all, runs after the typed handlers

WebApplication app = builder.Build();
app.MapSmtp2GoWebhook("/webhooks/smtp2go")
   .RequireBearer(builder.Configuration["Smtp2Go:Webhook:Token"]!)   // the webhook's auth_header_type: bearer value
   .RequireSmtp2GoSourceIp();                                        // optional, see below
app.Run();

public sealed class BounceHandler(ISuppressions suppressions) : IWebhookEventHandler<EmailBounceEvent>
{
    public Task HandleAsync(EmailBounceEvent bounce, CancellationToken cancellationToken)
    {
        return bounce.BounceType == BounceType.Hard ? suppressions.AddAsync(bounce.Recipient!, cancellationToken) : Task.CompletedTask;
    }
}
```

Without handler registration, an inline delegate receives every event and needs no `AddSmtp2GoWebhooks`:

```csharp
app.MapSmtp2GoWebhook("/webhooks/smtp2go", async (WebhookEvent evt, CancellationToken ct) =>
{
    if (evt is EmailWebhookEvent email) await store.SaveAsync(email, ct);
}).RequireBasicAuth("hook", builder.Configuration["Smtp2Go:Webhook:Password"]!);
```

Register the URL with `client.Webhooks.AddAsync` or `AddOrUpdateByUrlAsync` (see [Management](#management)), with `OutputFormat = WebhookOutputFormat.Json` and the matching `AuthHeaderType`/`AuthHeaderValue`.

### What the endpoint does

`MapSmtp2GoWebhook` maps a `POST` minimal-API endpoint (a plain `RequestDelegate`: no reflection-based binding, so it is trim- and AOT-safe) that runs these checks in order and stops at the first failure, before reading the body where it can:

| Step | Outcome |
| :-- | :-- |
| `RequireSmtp2GoSourceIp()` configured and the remote address is not one of `webhooks.smtp2go.com`'s | 403 (503 when the addresses cannot be resolved) |
| `RequireBasicAuth`/`RequireBearer` configured and no header value matches | 401 with a `WWW-Authenticate` challenge per configured scheme |
| Media type not in `AllowedContentTypes` (default `application/json`, `application/x-www-form-urlencoded`, `multipart/form-data`) or missing | 415 |
| Body longer than `MaxBodyBytes` (default 1 MiB; checked against `Content-Length` and again while reading a chunked body) | 413 |
| Body is not a JSON object or not a well-formed form | 400 |
| Handler throws | `ReturnStatusOnHandlerError`, default 500 (the exception is logged) |
| Handler completes | 200, empty body |

JSON goes through `WebhookPayloadParser.Parse`; urlencoded and multipart bodies are split by ASP.NET Core's form reader and go through `ParseForm`, so both formats yield the same `WebhookEvent` subtypes described under [Types](#types). The endpoint carries a `Smtp2GoWebhookMetadata` marker (`Pattern`, `UsesHandlerDispatch`) for filters and OpenAPI transformers and is excluded from API descriptions. The returned `Smtp2GoWebhookEndpointConventionBuilder` is an `IEndpointConventionBuilder`, so `WithName`, `RequireHost`, `AddEndpointFilter` and the rest still apply.

### Timeout and retries: return quickly

SMTP2GO waits 10 seconds for your response, then treats the delivery as failed and retries up to 35 times over 48 hours (5 attempts in the first 30 minutes, hourly for a day, then every 6 and 12 hours). Two consequences:

- Do the minimum in the handler: validate, store or enqueue, return. Anything slower than a couple of seconds (calling another API, sending mail, heavy database work) belongs on a queue or a background service. `HandleAsync` receives `HttpContext.RequestAborted` as its token.
- Decide what a handler failure means. With the default `ReturnStatusOnHandlerError = 500`, a thrown exception makes SMTP2GO redeliver the same callback later, which is right for transient failures (database down) and wrong for permanent ones (a payload your code cannot process will come back 35 times). Handlers should catch permanent failures themselves, log, and return; or set `ReturnStatusOnHandlerError = 200` per endpoint to acknowledge and drop every failed callback. Every callback carries `WebhookId` (`id`), so a store can treat redeliveries idempotently.

A 4xx from the checks above (401, 403, 413, 415, 400) is retried by SMTP2GO just like a 500; fix the configuration and the queued deliveries arrive.

### Handlers and dispatch order

`IWebhookEventHandler<TEvent>` (`Task HandleAsync(TEvent, CancellationToken)`) is registered explicitly with any lifetime; nothing is scanned. For each callback the dispatcher (`IWebhookEventDispatcher`, scoped, also resolvable to replay stored callbacks) invokes, sequentially and in registration order:

1. the handlers for the concrete type (`EmailBounceEvent`, `SmsStatusEvent`, ...),
2. then for each base type: `EmailOpenEvent` for a click, `EmailWebhookEvent` for every email event,
3. then `IWebhookEventHandler<WebhookEvent>`, the catch-all.

An `UnknownWebhookEvent` (an `event` value this library does not know) reaches only `IWebhookEventHandler<UnknownWebhookEvent>` and the catch-all. The first exception stops the chain. A callback that matches no handler is still acknowledged with 200 and logged at Debug (event id 502). Scoped handlers are resolved from the request's scope.

### Authentication

SMTP2GO signs nothing, so authentication is a shared secret in the `Authorization` header:

- `RequireBearer(token)` matches the webhook's `AuthHeaderType = Bearer`, `AuthHeaderValue = token`.
- `RequireBasicAuth(user, pass)` matches `AuthHeaderType = Basic` with `base64(user:pass)`, and also the older method of credentials in the webhook URL (`https://user:pass@host/path`), which SMTP2GO sends as the same header. Percent-encoded userinfo (`p%40ss` for `p@ss`) is accepted literal or decoded; the first colon separates user from password.
- Both methods have an overload taking `Func<IServiceProvider, ...>`, evaluated on every request, so the secret can come from `IOptionsMonitor`, a secret store or a rotating source without restarting.
- Calling several `Require*` methods accepts any of the configured credentials (both schemes, or two Basic pairs while rotating).

Comparisons use `CryptographicOperations.FixedTimeEquals`; the rejection log line (event id 510) names the presented scheme, never the value.

### Source address check

`RequireSmtp2GoSourceIp()` compares `HttpContext.Connection.RemoteIpAddress` with the A and AAAA records of `webhooks.smtp2go.com` (the documented delivery source). `Smtp2GoSourceIpResolver` resolves them on first use, caches for one hour (`TimeProvider`-driven, so it is testable), shares one lookup between concurrent requests, and after a failed refresh keeps serving the previous result for a minute before trying DNS again; with nothing cached the endpoint answers 503 and SMTP2GO retries. It is off by default and is defence in depth, not a replacement for a secret: anyone who can spoof or share those addresses still needs the header.

Behind a reverse proxy or load balancer the remote address is the proxy's unless `ForwardedHeadersMiddleware` runs before routing with `KnownProxies`/`KnownNetworks` configured; the package deliberately reads only `RemoteIpAddress` and never parses `X-Forwarded-For` itself. Register your own `ISmtp2GoSourceIpResolver` (a fixed list, a stub in tests) before `AddSmtp2GoWebhooks` to replace DNS, or derive from `Smtp2GoSourceIpResolver` and override `LookupAsync`.

### Options

`Smtp2GoWebhookOptions` is registered by `AddSmtp2GoWebhooks(configure)` (or `services.Configure<Smtp2GoWebhookOptions>(...)`) and applies to every mapped endpoint; each endpoint takes a copy that `.WithOptions(o => ...)` adjusts on its own.

| Option | Default | Effect |
| :-- | :-- | :-- |
| `MaxBodyBytes` | 1 048 576 | Longest body read; 413 beyond it. A callback is a few hundred bytes. |
| `AllowedContentTypes` | `application/json`, `application/x-www-form-urlencoded`, `multipart/form-data` | Media types accepted (parameters such as `charset` ignored, `+json` suffixes count as JSON); 415 otherwise. Clear and add one entry to accept a single format. |
| `ReturnStatusOnHandlerError` | 500 | Status when a handler throws: 500 makes SMTP2GO retry, 200 drops the callback. |
| `KnownCustomHeaders` | empty | The webhook's `headers` names, passed to `WebhookParserOptions.KnownCustomHeaders` so they land in `EmailWebhookEvent.CustomHeaders`. |

### Logging

Category `Scott.Mail.Smtp2Go.AspNetCore`, event ids in `Smtp2GoWebhookEventIds`; never a body, credential or recipient address:

| Id | Level | When |
| :-- | :-- | :-- |
| 500 `CallbackReceived` | Debug | Parsed; kind, wire event name, delivery id, path, media type. |
| 501 `CallbackHandled` | Information | Handler completed; elapsed time. |
| 502 `NoHandlerRegistered` | Debug | Dispatch found no handler for the type or its bases. |
| 510 `Unauthorized` | Warning | 401; the presented scheme. |
| 511 `SourceIpRejected` | Warning | 403; the remote address. |
| 512 `SourceIpResolutionFailed` | Error | 503; the exception. |
| 520 `UnsupportedMediaType` | Warning | 415; the media type. |
| 521 `PayloadTooLarge` | Warning | 413; the limit. |
| 522 `PayloadInvalid` | Warning | 400; the exception. |
| 530 `HandlerFailed` | Error | The handler's exception and the status returned. |
| 540 `SourceIpResolved` | Debug | DNS refreshed; address count and cache expiry. |
| 541 `SourceIpStaleCacheUsed` | Warning | DNS failed; the previous addresses are in use. |

### Demo receiver

`demo/Scott.Mail.Smtp2Go.Demo.WebhookReceiver` is the sample above with a logging catch-all and a bounce handler, plus an inline endpoint at `/webhooks/smtp2go/inline`. `Smtp2Go:Webhook:BearerToken` (appsettings, user secrets or `Smtp2Go__Webhook__BearerToken`) turns on the bearer check, `Smtp2Go:Webhook:RequireSourceIp=true` the address check.

```shell
dotnet run --project demo/Scott.Mail.Smtp2Go.Demo.WebhookReceiver
curl -i -X POST http://localhost:5080/webhooks/smtp2go -H "Content-Type: application/json" \
     --data @test/Scott.Mail.Smtp2Go.Tests.Shared/Fixtures/Webhooks/Live/bounce-hard.json
curl -i -X POST http://localhost:5080/webhooks/smtp2go -H "Content-Type: application/x-www-form-urlencoded" \
     --data @test/Scott.Mail.Smtp2Go.Tests.Shared/Fixtures/Webhooks/Live/delivered.form
```

Both return `200` and log the event; expose the port with a tunnel (for example `cloudflared tunnel --url http://localhost:5080`) and register the public URL with `client.Webhooks.AddAsync` to receive real callbacks.
