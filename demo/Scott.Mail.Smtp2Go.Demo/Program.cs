// Scott.Mail.Smtp2Go demo: send an email, find it in the archive, download the original back.
//
// Prerequisites (printed at startup): a live (not sandbox) API key on a paid plan, Email Archiving enabled for that key,
// and a verified sender. Configure through environment variables:
//   SMTP2GO_API_KEY    the API key
//   SMTP2GO_SENDER     a verified sender, for example "Alice <alice@example.com>"
//   SMTP2GO_RECIPIENT  where to send the demo email
//
// Run: dotnet run --project demo/Scott.Mail.Smtp2Go.Demo
using Scott.Mail.Smtp2Go;

Console.WriteLine("Scott.Mail.Smtp2Go demo: send, find in the archive, download the original.");
Console.WriteLine("Requires: a live (not sandbox) API key on a paid plan, Email Archiving enabled for that key, and a verified sender.");
Console.WriteLine();

string? apiKey = Environment.GetEnvironmentVariable("SMTP2GO_API_KEY");
string? sender = Environment.GetEnvironmentVariable("SMTP2GO_SENDER");
string? recipient = Environment.GetEnvironmentVariable("SMTP2GO_RECIPIENT");
if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(sender) || string.IsNullOrWhiteSpace(recipient))
{
    Console.Error.WriteLine("Usage: set SMTP2GO_API_KEY, SMTP2GO_SENDER and SMTP2GO_RECIPIENT, then run: dotnet run --project demo/Scott.Mail.Smtp2Go.Demo");
    return 2;
}

var client = new Smtp2GoClient(apiKey);

// 1. Send with a unique subject so the archive search finds exactly this email.
string subject = $"Scott.Mail.Smtp2Go demo {Guid.NewGuid():N}";
ApiResponse<EmailSendResult> sent = await client.Email.SendAsync(new EmailSendRequest
{
    Sender = sender,
    To = [recipient],
    Subject = subject,
    TextBody = "This email was sent by the Scott.Mail.Smtp2Go demo. It will be searched for in the archive and downloaded back.",
    HtmlBody = "<p>This email was sent by the <b>Scott.Mail.Smtp2Go</b> demo. It will be searched for in the archive and downloaded back.</p>",
});
EmailSendResult accepted = sent.EnsureAccepted();
string emailId = accepted.EmailId ?? throw new InvalidOperationException("The API accepted the email but returned no email_id.");
Console.WriteLine($"Sent    email_id={emailId} subject=\"{subject}\" request_id={sent.RequestId}");

// 2. Poll the archive every 15 seconds for up to 5 minutes; the docs say to allow around two minutes before sent mail appears in searches.
DateTimeOffset todayUtcMidnight = new(DateTime.UtcNow.Date, TimeSpan.Zero);
DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(5);
ArchivedEmail? archived = null;
while (archived is null)
{
    ApiResponse<ArchiveSearchResult> search = await client.Archive.SearchAsync(new ArchiveSearchRequest { Subject = subject, StartDate = todayUtcMidnight });
    archived = search.Data.Emails?.FirstOrDefault(e => string.Equals(e.EmailId, emailId, StringComparison.Ordinal));
    if (archived is not null)
    {
        break;
    }

    if (DateTimeOffset.UtcNow >= deadline)
    {
        Console.Error.WriteLine($"Gave up after 5 minutes: email_id {emailId} is not in the archive yet (last search returned {search.Data.EmailCount ?? 0} match(es), request_id {search.RequestId}). Is Email Archiving enabled for this API key?");
        return 1;
    }

    Console.WriteLine($"Waiting for the archive ({search.Data.EmailCount ?? 0} match(es) so far, request_id {search.RequestId}); next search in 15 s.");
    await Task.Delay(TimeSpan.FromSeconds(15));
}

// 3. Print the archived metadata and download the original.
Console.WriteLine($"Archived sent={archived.Sent:O} byte_count={archived.ByteCount} attachment_count={archived.AttachmentCount} username={archived.Username}");
Directory.CreateDirectory("downloaded");
string fileName = string.Concat(emailId.Select(c => Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 ? '_' : c)) + ".eml";
string path = Path.Combine("downloaded", fileName);
long bytes;
using (FileStream file = File.Create(path))
{
    bytes = await client.Archive.DownloadOriginalAsync(archived, file);
}

Console.WriteLine($"Downloaded {bytes} bytes to {Path.GetFullPath(path)}");

// 4. The by-id path.
ApiResponse<ArchivedEmail> byId = await client.Archive.GetAsync(emailId);
Console.WriteLine($"archive/email request_id={byId.RequestId} subject=\"{byId.Data.Subject}\" sent={byId.Data.Sent:O}");
return 0;
