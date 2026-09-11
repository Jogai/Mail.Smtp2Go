# Email archive

`client.Archive` covers `archive/search` and `archive/email`, plus downloading the original message. Email Archiving is an account-level feature of paid plans, switched on per API key or SMTP user in the app; the "Adjusting archives" documentation page describes that setting and has no API endpoint. With archiving off, searches simply return nothing.

## Prerequisites

- A live (not sandbox) API key on a paid plan with Email Archiving enabled for that key.
- A verified sender, if you want to send and then find the email.

## Search

```csharp
DateTimeOffset todayUtcMidnight = new(DateTime.UtcNow.Date, TimeSpan.Zero);
ApiResponse<ArchiveSearchResult> found = await client.Archive.SearchAsync(new ArchiveSearchRequest
{
    StartDate = todayUtcMidnight,         // inclusive; server default is today at midnight UTC
    EndDate = null,                       // exclusive; server default is now
    Subject = "Order confirmation",       // exact filters: Username, Recipient, Sender, EnvelopeFrom, Subject
    Headers = "X-Order-Id: 42",           // substring match on the raw headers
    Limit = 100,                          // 1 to 5,000; server default 5,000
});
foreach (ArchivedEmail email in found.Data.Emails ?? [])
{
    Console.WriteLine($"{email.Sent:u} {email.EmailId} {email.Subject} {email.ByteCount} bytes, {email.AttachmentCount} attachment(s)");
}
```

`ArchivedEmail` carries `EmailId`, `Sender`, `EnvelopeFrom`, `Recipient`, `To`, `Subject`, `Sent`, `Username`, `ByteCount`, `AttachmentCount`, `Attachments` (raw; the docs do not describe the items), `Headers` and `Url`. `SearchAllAsync` follows `continue_token` for as long as the server returns one (see [api-notes](api-notes.md): the response schema does not document the token).

### Indexing delay

Sent mail takes time to become searchable; the documentation says to allow around two minutes. Poll with a generous window, as the demo does (every 15 seconds for up to 5 minutes), and search by a unique subject or by `email_id` from the send response rather than assuming the first search hits.

## Get by id

```csharp
ApiResponse<ArchivedEmail> one = await client.Archive.GetAsync("1u0SwL-B9zBpi9ffUq-JAB2");
```

## Download the original

```csharp
using FileStream file = File.Create($"{one.Data.EmailId}.eml");
long bytes = await client.Archive.DownloadOriginalAsync(one.Data, file);
```

The original is fetched from `ArchivedEmail.Url` through the same `HttpClient` the API calls use, with the per-request timeout. The link is tried without the API key first; if the server answers 401 or 403 the request is repeated with the key (in the configured `AuthenticationScheme`). Other failures throw `Smtp2GoApiException` with the status code. The result is the raw RFC 5322 message, suitable for an `.eml` file or a MIME parser.

## Demo

`demo/Scott.Mail.Smtp2Go.Demo` is the full round trip: `dotnet run --project demo/Scott.Mail.Smtp2Go.Demo` with `SMTP2GO_API_KEY`, `SMTP2GO_SENDER` and `SMTP2GO_RECIPIENT` set sends an email with a unique subject, polls the archive, prints the metadata, writes `./downloaded/<email_id>.eml` and fetches the email by id. The same flow with assertions is `LiveArchiveTests` in the integration project (`[Trait("Category", "Live")]`).
