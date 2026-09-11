namespace Scott.Mail.Smtp2Go.Tests.Integration.Live;

/// <summary>The demo flow with assertions: send, find in the archive, download the original, fetch by id. Needs a live key with Email Archiving; see <see cref="LiveEnvironment"/>.</summary>
[Trait("Category", "Live")]
public class LiveArchiveTests
{
    [Fact]
    public async Task Sent_email_appears_in_the_archive_and_the_original_downloads()
    {
        Smtp2GoClient client = LiveEnvironment.CreateClientOrSkip();
        CancellationToken ct = TestContext.Current.CancellationToken;
        string subject = $"Scott.Mail.Smtp2Go live archive test {Guid.NewGuid():N}";

        ApiResponse<EmailSendResult> sent = await client.Email.SendAsync(new EmailSendRequest
        {
            Sender = LiveEnvironment.Sender!,
            To = [LiveEnvironment.Recipient!],
            Subject = subject,
            TextBody = "Sent by the Scott.Mail.Smtp2Go live archive test.",
        }, cancellationToken: ct);
        string emailId = sent.EnsureAccepted().EmailId ?? throw new InvalidOperationException("No email_id.");

        DateTimeOffset todayUtcMidnight = new(DateTime.UtcNow.Date, TimeSpan.Zero);
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(5);
        ArchivedEmail? archived = null;
        while (archived is null && DateTimeOffset.UtcNow < deadline)
        {
            ApiResponse<ArchiveSearchResult> search = await client.Archive.SearchAsync(new ArchiveSearchRequest { Subject = subject, StartDate = todayUtcMidnight }, cancellationToken: ct);
            archived = search.Data.Emails?.FirstOrDefault(e => e.EmailId == emailId);
            if (archived is null)
            {
                await Task.Delay(TimeSpan.FromSeconds(15), ct);
            }
        }

        archived.Should().NotBeNull(because: "the archive should list the email within five minutes");
        archived!.Subject.Should().Be(subject);
        archived.ByteCount.Should().BeGreaterThan(0);
        archived.Url.Should().StartWith("http");

        using MemoryStream original = new();
        long bytes = await client.Archive.DownloadOriginalAsync(archived, original, cancellationToken: ct);
        bytes.Should().BeGreaterThan(0);
        System.Text.Encoding.ASCII.GetString(original.ToArray()).Should().Contain("Subject:");

        ApiResponse<ArchivedEmail> byId = await client.Archive.GetAsync(emailId, cancellationToken: ct);
        byId.Data.EmailId.Should().Be(emailId);
    }
}
