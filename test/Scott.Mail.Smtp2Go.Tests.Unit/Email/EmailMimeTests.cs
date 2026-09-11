using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Email;

public class EmailMimeTests
{
    private const string RawMessage = "From: alice@example.com\r\nTo: bob@example.com\r\nSubject: Hi\r\n\r\nHello\r\n";

    [Fact]
    public void FromBytes_base64_encodes_the_message()
    {
        EmailMimeRequest request = EmailMimeRequest.FromBytes(Encoding.ASCII.GetBytes(RawMessage));

        Golden.AssertMatches(JsonSerializer.Serialize(request, Smtp2GoJsonContext.Default.EmailMimeRequest), "mime-minimal.json");
        Convert.FromBase64String(request.MimeEmail).Should().Equal(Encoding.ASCII.GetBytes(RawMessage));
    }

    [Fact]
    public void Full_request_serialises_schedule_and_fastaccept()
    {
        EmailMimeRequest request = EmailMimeRequest.FromBytes(Encoding.ASCII.GetBytes(RawMessage)) with
        {
            Schedule = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero),
            FastAccept = true,
        };

        Golden.AssertMatches(JsonSerializer.Serialize(request, Smtp2GoJsonContext.Default.EmailMimeRequest), "mime-full.json");
    }

    [Fact]
    public async Task SendMimeAsync_posts_to_email_mime_and_parses_the_send_result()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("email/mime", HttpStatusCode.OK, Fixture.Read("Email/send-response-scheduled.json"));
        Smtp2GoClient client = TestClient.Create(handler);

        ApiResponse<EmailSendResult> response = await client.Email.SendMimeAsync(EmailMimeRequest.FromBytes(Encoding.ASCII.GetBytes(RawMessage)));

        handler.LastRequest.Endpoint!.Path.Should().Be("email/mime");
        handler.LastRequest.Uri.AbsolutePath.Should().EndWith("/v3/email/mime");
        Golden.AssertMatches(handler.LastRequest.Body!, "mime-minimal.json");
        response.Data.ScheduleId.Should().Be("188262b6-f6cc-4c98-bbe6-84c39d1c0ef4");
        response.EnsureAccepted().IsScheduled.Should().BeTrue();
    }

    [Fact]
    public async Task DefaultFastAccept_applies_to_mime_sends_too()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler, o => o.DefaultFastAccept = true);

        await client.Email.SendMimeAsync(new EmailMimeRequest { MimeEmail = "QQ==" });
        await client.Email.SendMimeAsync(new EmailMimeRequest { MimeEmail = "QQ==", FastAccept = false });

        JsonNode.Parse(handler.Requests[0].Body!)!["fastaccept"]!.GetValue<bool>().Should().BeTrue();
        JsonNode.Parse(handler.Requests[1].Body!)!["fastaccept"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public async Task Null_request_is_rejected()
    {
        Smtp2GoClient client = TestClient.Create(new FakeHttpMessageHandler());

        Func<Task> act = () => client.Email.SendMimeAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
        Func<EmailMimeRequest> fromNull = () => EmailMimeRequest.FromBytes(null!);
        fromNull.Should().Throw<ArgumentNullException>();
    }
}
