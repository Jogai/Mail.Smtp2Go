# Webhooks

Two halves: managing webhooks through the API (`client.Webhooks`) and parsing the callbacks SMTP2GO posts to your endpoint (`Scott.Mail.Smtp2Go.Webhooks.WebhookPayloadParser`). Receiving them in ASP.NET Core with `MapSmtp2GoWebhook` is the `Scott.Mail.Smtp2Go.AspNetCore` package (plan 06), which builds on the parser described here.

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
