using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Email;

public class EmailRequestValidatorTests
{
    private static readonly Endpoint s_send = EndpointTable.Get("email/send");

    private static EmailSendRequest Valid()
    {
        return new EmailSendRequest { Sender = "alice@example.com", To = ["bob@example.com"], Subject = "s", TextBody = "t" };
    }

    private static List<string> Errors(object request, Endpoint? endpoint = null)
    {
        List<string> errors = [];
        ((IRequestValidator)request).Validate(endpoint ?? s_send, errors);
        return errors;
    }

    private static List<EmailAddress> Recipients(int count)
    {
        return Enumerable.Range(0, count).Select(i => new EmailAddress($"r{i}@example.com")).ToList();
    }

    [Fact]
    public void Valid_request_has_no_errors()
    {
        Errors(Valid()).Should().BeEmpty();
        Errors(Valid() with { HtmlBody = "<p>h</p>", TextBody = null, Cc = Recipients(100), Bcc = Recipients(100), To = Recipients(100) }).Should().BeEmpty();
        Errors(Valid() with { TextBody = null, TemplateId = "tpl" }).Should().BeEmpty();
        Errors(Valid() with { Schedule = DateTimeOffset.UtcNow.AddHours(1) }).Should().BeEmpty();
        Errors(Valid() with { Attachments = [Attachment.FromBytes("a.txt", [1, 2, 3])], Inlines = [Attachment.FromUrl("i.png", new Uri("https://example.com/i.png"))] }).Should().BeEmpty();
        Errors(Valid() with { CustomHeaders = [new CustomHeader("X-Campaign", "spring"), new CustomHeader("Reply-To", "a@example.com")] }).Should().BeEmpty();
    }

    [Fact]
    public void Sender_is_required()
    {
        Errors(Valid() with { Sender = default }).Should().ContainSingle().Which.Should().Be("sender is required.");
    }

    [Fact]
    public void To_must_have_at_least_one_recipient()
    {
        Errors(Valid() with { To = [] }).Should().ContainSingle().Which.Should().Be("to must contain at least one recipient.");
        Errors(Valid() with { To = null! }).Should().ContainSingle().Which.Should().Be("to must contain at least one recipient.");
    }

    [Theory]
    [InlineData("to")]
    [InlineData("cc")]
    [InlineData("bcc")]
    public void Each_recipient_field_allows_at_most_100(string field)
    {
        IReadOnlyList<EmailAddress> tooMany = Recipients(101);
        EmailSendRequest request = field switch
        {
            "to" => Valid() with { To = tooMany },
            "cc" => Valid() with { Cc = tooMany },
            _ => Valid() with { Bcc = tooMany },
        };

        Errors(request).Should().ContainSingle().Which.Should().Be($"{field} has 101 recipients; the limit is 100.");
    }

    [Fact]
    public void Empty_cc_and_bcc_are_allowed()
    {
        Errors(Valid() with { Cc = [], Bcc = [] }).Should().BeEmpty();
    }

    [Fact]
    public void Default_addresses_in_recipient_lists_are_reported_with_their_index()
    {
        Errors(Valid() with { Cc = [new EmailAddress("a@example.com"), default] }).Should().ContainSingle().Which.Should().Be("cc[1] is empty.");
    }

    [Fact]
    public void A_body_or_template_is_required()
    {
        Errors(Valid() with { TextBody = " " }).Should().ContainSingle().Which.Should().Be("one of text_body, html_body or template_id is required.");
    }

    [Theory]
    [InlineData("Content-Type")]
    [InlineData("content-transfer-encoding")]
    [InlineData("MIME-Version ")]
    public void Disallowed_custom_headers_are_rejected(string name)
    {
        Errors(Valid() with { CustomHeaders = [new CustomHeader(name, "x")] })
            .Should().ContainSingle().Which.Should().StartWith($"custom_headers[0] '{name}' is not allowed");
    }

    [Fact]
    public void Custom_headers_need_a_name()
    {
        Errors(Valid() with { CustomHeaders = [new CustomHeader("X-Ok", "1"), new CustomHeader(" ", "x")] })
            .Should().ContainSingle().Which.Should().Be("custom_headers[1] must have a header name.");
    }

    [Fact]
    public void Attachments_need_exactly_one_of_fileblob_or_url()
    {
        Attachment neither = new() { Filename = "a.txt" };
        Attachment both = new() { Filename = "b.txt", Fileblob = "QQ==", Url = "https://example.com/b" };

        Errors(Valid() with { Attachments = [neither], Inlines = [both] }).Should().Equal(
            "attachments[0] must set exactly one of fileblob or url.",
            "inlines[0] must set exactly one of fileblob or url.");
    }

    [Fact]
    public void Attachment_blob_must_be_base64_and_filename_present()
    {
        Errors(Valid() with { Attachments = [new Attachment { Filename = " ", Fileblob = "not base64!" }] }).Should().Equal(
            "attachments[0] must have a filename.",
            "attachments[0].fileblob must be Base64-encoded.");
    }

    [Theory]
    [InlineData("QQ==", true)]
    [InlineData("QUJD", true)]
    [InlineData("QUJDRA==", true)]
    [InlineData("QUJD\r\nRA==", true)]
    [InlineData("", false)]
    [InlineData("QUJ", false)]
    [InlineData("QUJD=A==", false)]
    [InlineData("QUJDRA===", false)]
    [InlineData("QUJD RA!=", false)]
    [InlineData("QUJDR-==", false)]
    public void Base64_check_accepts_the_alphabet_and_padding_only(string value, bool expected)
    {
        EmailRequestValidator.IsBase64(value).Should().Be(expected);
    }

    [Fact]
    public void Schedule_must_be_in_the_future()
    {
        Errors(Valid() with { Schedule = DateTimeOffset.UtcNow.AddMinutes(-1) }).Should().ContainSingle().Which.Should().Be("schedule must be in the future.");
    }

    [Fact]
    public void Schedule_must_be_at_most_three_days_ahead()
    {
        Errors(Valid() with { Schedule = DateTimeOffset.UtcNow.AddDays(3).AddMinutes(1) }).Should().ContainSingle().Which.Should().Be("schedule must be at most 3 days ahead.");
        Errors(Valid() with { Schedule = DateTimeOffset.UtcNow.AddDays(3).AddMinutes(-1) }).Should().BeEmpty();
    }

    [Fact]
    public void Mime_request_needs_a_base64_message_and_a_valid_schedule()
    {
        Errors(new EmailMimeRequest { MimeEmail = " " }).Should().Equal("mime_email is required.");
        Errors(new EmailMimeRequest { MimeEmail = "%%%", Schedule = DateTimeOffset.UtcNow.AddDays(-1) }).Should().Equal("mime_email must be Base64-encoded.", "schedule must be in the future.");
        Errors(new EmailMimeRequest { MimeEmail = "QQ==", Schedule = DateTimeOffset.UtcNow.AddHours(2) }).Should().BeEmpty();
    }

    [Fact]
    public void Batch_needs_one_to_one_thousand_emails()
    {
        Errors(new EmailBatchRequest { Emails = [] }).Should().Equal("emails must contain at least one email.");
        Errors(new EmailBatchRequest { Emails = Enumerable.Repeat(Valid(), 1001).ToList() }).Should().Equal("emails has 1001 entries; the limit is 1000.");
        Errors(new EmailBatchRequest { Emails = Enumerable.Repeat(Valid(), 1000).ToList() }).Should().BeEmpty();
    }

    [Fact]
    public void Batch_items_are_validated_individually_with_their_index()
    {
        EmailBatchRequest batch = new() { Emails = [Valid(), Valid() with { To = [] }, null!, Valid() with { TextBody = null }] };

        Errors(batch).Should().Equal(
            "emails[1].to must contain at least one recipient.",
            "emails[2] is null.",
            "emails[3].one of text_body, html_body or template_id is required.");
    }

    [Fact]
    public void Scheduled_search_paging_values_must_be_positive()
    {
        Errors(new ScheduledEmailSearchRequest { Limit = 0, Page = -1 }).Should().Equal("limit must be positive.", "page must be 1 or greater.");
        Errors(new ScheduledEmailSearchRequest { Limit = 10, Page = 1 }).Should().BeEmpty();
        Errors(new ScheduledEmailSearchRequest()).Should().BeEmpty();
    }

    [Fact]
    public void Email_search_limit_is_one_to_five_thousand()
    {
#pragma warning disable CS0618
        Errors(new EmailSearchRequest { Limit = 5001 }).Should().Equal("limit must be between 1 and 5000.");
        Errors(new EmailSearchRequest { Limit = 0 }).Should().Equal("limit must be between 1 and 5000.");
        Errors(new EmailSearchRequest { Limit = 5000 }).Should().BeEmpty();
#pragma warning restore CS0618
    }

    [Fact]
    public void All_errors_are_collected_at_once()
    {
        EmailSendRequest request = new()
        {
            Sender = default,
            To = [],
            CustomHeaders = [new CustomHeader("MIME-Version", "1.0")],
            Attachments = [new Attachment { Filename = "a" }],
            Schedule = DateTimeOffset.UtcNow.AddDays(-1),
        };

        Errors(request).Should().Equal(
            "sender is required.",
            "to must contain at least one recipient.",
            "one of text_body, html_body or template_id is required.",
            "custom_headers[0] 'MIME-Version' is not allowed; the API rejects Content-Type, Content-Transfer-Encoding, MIME-Version.",
            "attachments[0] must set exactly one of fileblob or url.",
            "schedule must be in the future.");
    }

    [Fact]
    public async Task Client_throws_Smtp2GoValidationException_with_every_error_and_sends_nothing()
    {
        FakeHttpMessageHandler handler = new();
        RecordingDiagnostics diagnostics = new();
        Smtp2GoClient client = TestClient.Create(handler, diagnostics: diagnostics);

        Func<Task> act = () => client.Email.SendAsync(Valid() with { To = Recipients(101), TextBody = null });

        Smtp2GoValidationException exception = (await act.Should().ThrowAsync<Smtp2GoValidationException>()).Which;
        exception.Errors.Should().Equal("to has 101 recipients; the limit is 100.", "one of text_body, html_body or template_id is required.");
        exception.Message.Should().Contain("email/send");
        handler.Requests.Should().BeEmpty();
        diagnostics.ValidationFailures.Should().ContainSingle();
    }

    [Fact]
    public async Task Validation_is_skipped_when_ClientSideValidation_is_off()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler, o => o.ClientSideValidation = false);

        await client.Email.SendAsync(Valid() with { TextBody = null });

        handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task Batch_and_mime_are_validated_through_the_client_too()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler);

        Func<Task> batch = () => client.Email.SendBatchAsync(new EmailBatchRequest { Emails = [] });
        Func<Task> mime = () => client.Email.SendMimeAsync(new EmailMimeRequest { MimeEmail = "" });

        (await batch.Should().ThrowAsync<Smtp2GoValidationException>()).Which.Errors.Should().Equal("emails must contain at least one email.");
        (await mime.Should().ThrowAsync<Smtp2GoValidationException>()).Which.Errors.Should().Equal("mime_email is required.");
        handler.Requests.Should().BeEmpty();
    }
}
