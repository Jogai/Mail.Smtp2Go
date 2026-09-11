using System.Text;
using System.Text.Json;

namespace Scott.Mail.Smtp2Go.Tests.Integration.Email;

/// <summary>Real calls against the SMTP2GO sandbox, which accepts and does not deliver. Skipped without a sandbox key; see <see cref="SandboxKey"/>.</summary>
[Trait("Category", "Sandbox")]
public class SandboxEmailTests
{
    private const string Sender = "Scott.Mail.Smtp2Go tests <sandbox@example.com>";
    private const string Recipient = "recipient@example.com";

    [Fact]
    public async Task Send_is_accepted()
    {
        Smtp2GoClient client = SandboxKey.CreateClient();

        ApiResponse<EmailSendResult> response = await client.Email.SendAsync(new EmailSendRequest
        {
            Sender = Sender,
            To = [Recipient],
            Subject = "Sandbox send",
            TextBody = "Sent by the Scott.Mail.Smtp2Go integration tests.",
            HtmlBody = "<p>Sent by the <b>Scott.Mail.Smtp2Go</b> integration tests.</p>",
            CustomHeaders = [new CustomHeader("X-Test-Run", Guid.NewGuid().ToString("N"))],
            Attachments = [Attachment.FromBytes("hello.txt", Encoding.UTF8.GetBytes("hello"))],
        }, cancellationToken: TestContext.Current.CancellationToken);

        response.RequestId.Should().NotBeNullOrWhiteSpace();
        EmailSendResult result = response.EnsureAccepted();
        result.EmailId.Should().NotBeNullOrWhiteSpace();
        result.Failed.GetValueOrDefault().Should().Be(0);
    }

    [Fact]
    public async Task Fastaccept_send_returns_only_an_email_id()
    {
        Smtp2GoClient client = SandboxKey.CreateClient(o => o.DefaultFastAccept = true);

        ApiResponse<EmailSendResult> response = await client.Email.SendAsync(Sender, Recipient, "Sandbox fastaccept", "<p>Hi</p>", "Hi", TestContext.Current.CancellationToken);

        response.EnsureAccepted().EmailId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Mime_send_is_accepted()
    {
        Smtp2GoClient client = SandboxKey.CreateClient();
        string mime = "From: " + Sender + "\r\nTo: " + Recipient + "\r\nSubject: Sandbox MIME\r\nMIME-Version: 1.0\r\nContent-Type: text/plain; charset=utf-8\r\n\r\nSent by the integration tests.\r\n";

        ApiResponse<EmailSendResult> response = await client.Email.SendMimeAsync(EmailMimeRequest.FromBytes(Encoding.UTF8.GetBytes(mime)), cancellationToken: TestContext.Current.CancellationToken);

        response.EnsureAccepted().EmailId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Batch_of_two_returns_one_item_per_email_in_order()
    {
        Smtp2GoClient client = SandboxKey.CreateClient();

        ApiResponse<IReadOnlyList<EmailBatchItem>> response = await client.Email.SendBatchAsync(new EmailBatchRequest
        {
            Emails =
            [
                new EmailSendRequest { Sender = Sender, To = [Recipient], Subject = "Sandbox batch 1", TextBody = "one" },
                new EmailSendRequest { Sender = Sender, To = [Recipient], Subject = "Sandbox batch 2", TextBody = "two" },
            ],
        }, cancellationToken: TestContext.Current.CancellationToken);

        response.Data.Should().HaveCount(2).And.OnlyContain(item => !string.IsNullOrWhiteSpace(item.EmailId) || !string.IsNullOrWhiteSpace(item.ScheduleId));
    }

    [Fact]
    public async Task Scheduled_send_can_be_found_and_removed()
    {
        Smtp2GoClient client = SandboxKey.CreateClient();
        string marker = "Sandbox scheduled " + Guid.NewGuid().ToString("N");

        ApiResponse<EmailSendResult> sent = await client.Email.SendAsync(new EmailSendRequest
        {
            Sender = Sender,
            To = [Recipient],
            Subject = marker,
            TextBody = "Scheduled by the integration tests; removed again by them.",
            Schedule = DateTimeOffset.UtcNow.AddHours(2),
        }, cancellationToken: TestContext.Current.CancellationToken);
        string scheduleId = sent.EnsureAccepted().ScheduleId ?? throw new InvalidOperationException("The sandbox did not return a schedule_id.");

        ApiResponse<IReadOnlyList<ScheduledEmail>> found = await client.Email.SearchScheduledAsync(new ScheduledEmailSearchRequest { ScheduleId = scheduleId }, cancellationToken: TestContext.Current.CancellationToken);
        List<ScheduledEmail> all = [];
        await foreach (ScheduledEmail item in client.Email.SearchScheduledAllAsync(new ScheduledEmailSearchRequest { SearchSubject = marker, Limit = 10 }, cancellationToken: TestContext.Current.CancellationToken))
        {
            all.Add(item);
        }

        ApiResponse<JsonElement> removed = await client.Email.RemoveScheduledAsync(scheduleId, cancellationToken: TestContext.Current.CancellationToken);

        found.Data.Should().ContainSingle().Which.ScheduleId.Should().Be(scheduleId);
        all.Should().Contain(item => item.ScheduleId == scheduleId);
        removed.RequestId.Should().NotBeNullOrWhiteSpace();
    }
}
