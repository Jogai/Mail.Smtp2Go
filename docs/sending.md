# Sending email

`client.Email` covers the whole `email/*` family: `email/send`, `email/mime`, `email/batch`, `email/scheduled/search`, `email/scheduled/remove` and the deprecated `email/search`. Every request model has an explicit wire name per property, nulls are never serialised, and the documented limits are checked client-side before anything is sent (turn that off with `Smtp2GoClientOptions.ClientSideValidation = false`).

## The three send paths

| Method | Endpoint | Use when |
| :-- | :-- | :-- |
| `SendAsync(EmailSendRequest)` | `email/send` | You have the parts of the email: sender, recipients, subject, bodies, headers, attachments, or a template id and data. |
| `SendMimeAsync(EmailMimeRequest)` | `email/mime` | You already have a complete MIME message (for example from MimeKit) and want SMTP2GO to relay it as is. |
| `SendBatchAsync(EmailBatchRequest)` | `email/batch` | You have up to 1,000 emails, each with its own recipients, template data or schedule, and want one HTTP call. |

Only `Sender` and `To` are required on `EmailSendRequest`; the API requires a `TextBody`, an `HtmlBody` or a `TemplateId`, which the validator enforces. `EmailAddress` parses `Name <address>` and bare addresses and quotes display names that need it; a `string` converts implicitly.

```csharp
using Scott.Mail.Smtp2Go;

var client = new Smtp2GoClient("api-...");

var response = await client.Email.SendAsync(new EmailSendRequest
{
    Sender = "Alice <alice@example.com>",
    To = ["bob@example.com", new EmailAddress("carol@example.com", "Smith, Carol")],
    Cc = ["dave@example.com"],
    Subject = "Hello",
    HtmlBody = "<p>Hello from Scott.Mail.Smtp2Go</p>",
    TextBody = "Hello from Scott.Mail.Smtp2Go",
    CustomHeaders = [new CustomHeader("X-Campaign", "spring")],
});

Console.WriteLine($"{response.Data.Succeeded} succeeded, {response.Data.Failed} failed, email {response.Data.EmailId}");
```

For the common one-recipient case the extension methods build the request for you:

```csharp
await client.Email.SendAsync("Alice <alice@example.com>", "bob@example.com", "Hello", htmlBody: "<p>Hi</p>", textBody: "Hi");
await client.Email.SendTemplateAsync("alice@example.com", "bob@example.com", "welcome", new Dictionary<string, object?> { ["first_name"] = "Bob" });
```

MIME messages are sent Base64-encoded. `EmailMimeRequest.FromBytes` encodes the raw bytes; `Schedule` and `FastAccept` work as they do for `SendAsync`.

```csharp
byte[] raw = File.ReadAllBytes("message.eml");
var mime = await client.Email.SendMimeAsync(EmailMimeRequest.FromBytes(raw));
```

A batch returns one `EmailBatchItem` per email, in request order, with either `EmailId` (sent) or `ScheduleId` (queued):

```csharp
var batch = await client.Email.SendBatchAsync(new EmailBatchRequest
{
    Emails =
    [
        new EmailSendRequest { Sender = "alice@example.com", To = ["bob@example.com"], TemplateId = "reminder", TemplateData = new Dictionary<string, object?> { ["when"] = "today" } },
        new EmailSendRequest { Sender = "alice@example.com", To = ["carol@example.com"], TemplateId = "reminder", TemplateData = new Dictionary<string, object?> { ["when"] = "tomorrow" }, Schedule = DateTimeOffset.UtcNow.AddDays(1) },
    ],
});
foreach (var item in batch.Data)
{
    Console.WriteLine(item.IsScheduled ? $"queued {item.ScheduleId}" : $"sent {item.EmailId}");
}
```

## Check the result: `EnsureAccepted`

`email/send` and `email/mime` answer `200 OK` even when every recipient failed; the outcome is in `Data.Failed` and `Data.Failures`. The client never turns that into an exception by itself. Call `EnsureAccepted()` when you want one:

```csharp
try
{
    EmailSendResult result = (await client.Email.SendAsync(request)).EnsureAccepted();
    Console.WriteLine($"sent as {result.EmailId}");
}
catch (Smtp2GoSendException ex)
{
    // ex.Failures lists one message per failed recipient; ex.RequestId is the server's request id; ex.Result is the full result.
}
```

`EnsureAccepted` on the `ApiResponse<EmailSendResult>` carries the request id into the exception; `EmailSendResult.EnsureAccepted()` works on the result alone. Both return the result unchanged when nothing failed, so they chain.

Non-success HTTP responses (invalid key, permission denied, a rejected body) throw `Smtp2GoApiException` or a subtype as for every endpoint; see [errors](errors.md).

## `fastaccept`

With `fastaccept: true` SMTP2GO accepts the email immediately and delivers in the background; the response then carries only `email_id` and no `succeeded`/`failed` counts (`EmailSendResult.Succeeded`, `Failed` and `Failures` are `null`). SMTP2GO recommends it and says it will become the default, so this library exposes it in three places:

- `EmailSendRequest.FastAccept` / `EmailMimeRequest.FastAccept` per request.
- `Smtp2GoClientOptions.DefaultFastAccept`: applied to every `SendAsync` and `SendMimeAsync` whose request leaves `FastAccept` null. Set it once, application-wide.
- Left null everywhere, the server default applies and nothing is sent for the field.

The batch endpoint does not document `fastaccept` per email, so `DefaultFastAccept` is not applied to batch items.

## Scheduling

Set `Schedule` (a `DateTimeOffset`; it is sent as ISO-8601 UTC, whatever offset you give) to queue the email instead of sending it. The value must be in the future and at most three days ahead, which the validator checks; SMTP2GO queues up to 50,000 emails per account. The response carries `ScheduleId` instead of `EmailId` (`EmailSendResult.IsScheduled`).

```csharp
var queued = (await client.Email.SendAsync(request with { Schedule = DateTimeOffset.UtcNow.AddHours(6) })).EnsureAccepted();

// Find queued emails: by id, or by subject/recipient/sender, one page at a time (limit defaults to 1,000 server-side) ...
var page = await client.Email.SearchScheduledAsync(new ScheduledEmailSearchRequest { ScheduleId = queued.ScheduleId });

// ... or every page.
await foreach (ScheduledEmail item in client.Email.SearchScheduledAllAsync(new ScheduledEmailSearchRequest { SearchSender = "alice@example.com", Limit = 100 }))
{
    Console.WriteLine($"{item.ScheduleId} {item.Schedule:u} {item.Subject} -> {item.Recipients}");
}

// Cancel delivery.
await client.Email.RemoveScheduledAsync(queued.ScheduleId!);
```

`SearchScheduledAllAsync` increments `page` until a page comes back empty (or shorter than `Limit`); each page is one call, made as you enumerate.

## Attachments and inline images

`Attachment` needs only a `Filename`; the content is either a Base64 `Fileblob` or a `Url` SMTP2GO fetches at send time (cached for 24 hours). `Mimetype` is optional and guessed from the extension when you use a factory. `Inlines` use the same type; reference them from the HTML body as `cid:filename`.

```csharp
Attachments =
[
    await Attachment.FromFileAsync("invoice.pdf"),                              // reads asynchronously now, not during serialisation
    Attachment.FromBytes("data.csv", csvBytes),
    Attachment.FromBase64("photo.jpg", base64, "image/jpeg"),
    Attachment.FromUrl("brochure.pdf", new Uri("https://example.com/brochure.pdf")),
    await Attachment.FromStreamAsync("export.zip", zipStream),
],
Inlines = [Attachment.FromBytes("logo.png", logoBytes)],
HtmlBody = "<p><img src=\"cid:logo.png\"></p>",
```

The validator requires exactly one of `Fileblob`/`Url` per attachment and checks that a blob is well-formed Base64. The whole request body may be up to 50 MB.

## Templates

Set `TemplateId` and `TemplateData`; `Subject`, `HtmlBody` and `TextBody` are then ignored by SMTP2GO and may be omitted. `TemplateData` is an `IReadOnlyDictionary<string, object?>` serialised without reflection, so values must be strings, numbers, booleans, string arrays, nested `Dictionary<string, object?>`, `JsonElement` or `JsonNode`; other object types are rejected in trimmed and AOT builds. Without a template, `TemplateData` variables can still be used in `Subject`.

## Recipient and size limits

- `To`, `Cc` and `Bcc` take up to 100 addresses each; `To` needs at least one.
- `custom_headers` must not contain `Content-Type`, `Content-Transfer-Encoding` or `MIME-Version`.
- A batch holds 1 to 1,000 emails; each is validated individually and errors are reported as `emails[3].to ...`.
- Request bodies are limited to 50 MB on `email/send`, `email/mime` and `email/batch`, and 1 MB on the search and remove endpoints.

Every violation found is reported at once in `Smtp2GoValidationException.Errors`, before any network activity.

## Deprecated: `email/search`

`SearchAsync(EmailSearchRequest)` still calls `email/search` (20 calls per minute) with the documented fields, but SMTP2GO has deprecated the endpoint and both the method and its models are marked `[Obsolete]`. Each returned email is a raw `JsonElement`. Use the activity search once it lands (plan 04).
