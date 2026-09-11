using System.Net;
using System.Text.Json.Nodes;
using Scott.Mail.Smtp2Go.Tests.Shared;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Email;

public class EmailClientSendTests
{
    private static EmailSendRequest Minimal()
    {
        return new EmailSendRequest { Sender = "Alice <alice@example.com>", To = ["bob@example.com"], Subject = "Hello", TextBody = "Plain text." };
    }

    [Fact]
    public async Task SendAsync_posts_the_serialised_request_to_email_send_and_parses_the_result()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("email/send", HttpStatusCode.OK, Fixture.Read("Email/send-response-ok.json"));
        Smtp2GoClient client = TestClient.Create(handler);

        ApiResponse<EmailSendResult> response = await client.Email.SendAsync(Minimal());

        RecordedRequest request = handler.LastRequest;
        request.Method.Should().Be(HttpMethod.Post);
        request.Uri.AbsolutePath.Should().EndWith("/v3/email/send");
        request.Endpoint!.Path.Should().Be("email/send");
        request.Header("X-Smtp2go-Api-Key").Should().Be(TestClient.ApiKey);
        Golden.AssertMatches(request.Body!, "send-minimal.json");
        response.RequestId.Should().Be("aa253464-0bd0-467a-b24b-6159dcd7be60");
        response.Data.EmailId.Should().Be("1u0SwL-B9zBpi9ffUq-JAB2");
        response.EnsureAccepted().Succeeded.Should().Be(1);
    }

    [Fact]
    public async Task DefaultFastAccept_fills_an_unset_fastaccept_but_never_overrides_an_explicit_one()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler, o => o.DefaultFastAccept = true);

        await client.Email.SendAsync(Minimal());
        await client.Email.SendAsync(Minimal() with { FastAccept = false });

        JsonNode.Parse(handler.Requests[0].Body!)!["fastaccept"]!.GetValue<bool>().Should().BeTrue();
        JsonNode.Parse(handler.Requests[1].Body!)!["fastaccept"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public async Task Without_DefaultFastAccept_the_field_is_omitted()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler);

        await client.Email.SendAsync(Minimal());

        handler.LastRequest.Body.Should().NotContain("fastaccept");
    }

    [Fact]
    public async Task Api_errors_surface_as_Smtp2GoApiException()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("email/send", HttpStatusCode.BadRequest, Fixture.Read("Transport/error-nested-permission.json"));
        Smtp2GoClient client = TestClient.Create(handler);

        Func<Task> act = () => client.Email.SendAsync(Minimal());

        await act.Should().ThrowAsync<Smtp2GoApiException>();
    }

    [Fact]
    public async Task Request_options_are_honoured()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler);

        await client.Email.SendAsync(Minimal(), new RequestOptions { Region = Region.EU, ApiKeyOverride = "api-override", AllowRetry = true });

        handler.LastRequest.Uri.Host.Should().Be(RegionEndpoints.EU.Host);
        handler.LastRequest.Header("X-Smtp2go-Api-Key").Should().Be("api-override");
        handler.LastRequest.AllowRetry.Should().BeTrue();
    }

    [Fact]
    public async Task Null_request_is_rejected_before_sending()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler);

        Func<Task> act = () => client.Email.SendAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_extension_builds_a_single_recipient_request()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler);

        await client.Email.SendAsync("Alice <alice@example.com>", "bob@example.com", "Hello", "<p>Hi</p>", "Hi", TestContext.Current.CancellationToken);

        JsonNode body = JsonNode.Parse(handler.LastRequest.Body!)!;
        body["sender"]!.GetValue<string>().Should().Be("Alice <alice@example.com>");
        body["to"]!.AsArray().Select(n => n!.GetValue<string>()).Should().Equal("bob@example.com");
        body["subject"]!.GetValue<string>().Should().Be("Hello");
        body["html_body"]!.GetValue<string>().Should().Be("<p>Hi</p>");
        body["text_body"]!.GetValue<string>().Should().Be("Hi");
        body.AsObject().Select(p => p.Key).Should().Equal("sender", "to", "subject", "html_body", "text_body");
    }

    [Fact]
    public async Task SendTemplateAsync_extension_builds_a_template_request()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler);

        await client.Email.SendTemplateAsync("alice@example.com", "bob@example.com", "welcome", new Dictionary<string, object?> { ["first_name"] = "Bob" }, TestContext.Current.CancellationToken);

        Golden.AssertMatches(handler.LastRequest.Body!, "send-template.json");
    }

    [Fact]
    public void Extensions_guard_their_arguments()
    {
        Func<Task> nullClient = () => EmailClientExtensions.SendAsync(null!, "a@example.com", "b@example.com", "s", "h");
        Func<Task> blankTemplate = () => TestClient.Create(new FakeHttpMessageHandler()).Email.SendTemplateAsync("a@example.com", "b@example.com", " ");

        nullClient.Should().ThrowAsync<ArgumentNullException>();
        blankTemplate.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Email_client_is_exposed_on_the_interface_and_is_stable()
    {
        Smtp2GoClient client = TestClient.Create(new FakeHttpMessageHandler());

        typeof(ISmtp2GoClient).GetProperty(nameof(ISmtp2GoClient.Email))!.PropertyType.Should().Be<IEmailClient>();
        client.Email.Should().NotBeNull().And.BeSameAs(client.Email);
    }

    [Fact]
    public async Task SendAsync_reports_the_recipient_counts_to_diagnostics()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("email/send", HttpStatusCode.OK, Fixture.Read("Email/send-response-failed.json"));
        RecordingDiagnostics diagnostics = new();
        Smtp2GoClient client = TestClient.Create(handler, diagnostics: diagnostics);

        await client.Email.SendAsync(Minimal());

        diagnostics.EmailResults.Should().Equal((Succeeded: 1, Failed: 1));
    }

    [Fact]
    public async Task SendMimeAsync_reports_the_recipient_counts_to_diagnostics()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("email/mime", HttpStatusCode.OK, Fixture.Read("Email/send-response-ok.json"));
        RecordingDiagnostics diagnostics = new();
        Smtp2GoClient client = TestClient.Create(handler, diagnostics: diagnostics);

        await client.Email.SendMimeAsync(new EmailMimeRequest { MimeEmail = "QQ==" });

        diagnostics.EmailResults.Should().Equal((Succeeded: 1, Failed: 0));
    }

    [Fact]
    public async Task A_fastaccept_response_without_counts_reports_nothing()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("email/send", HttpStatusCode.OK, Fixture.Read("Email/send-response-fastaccept.json"));
        RecordingDiagnostics diagnostics = new();
        Smtp2GoClient client = TestClient.Create(handler, diagnostics: diagnostics);

        await client.Email.SendAsync(Minimal() with { FastAccept = true });

        diagnostics.EmailResults.Should().BeEmpty();
    }

    [Fact]
    public async Task Batch_items_carry_no_counts_so_nothing_is_reported()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("email/batch", HttpStatusCode.OK, Fixture.Read("Email/batch-response.json"));
        RecordingDiagnostics diagnostics = new();
        Smtp2GoClient client = TestClient.Create(handler, diagnostics: diagnostics);

        await client.Email.SendBatchAsync(new EmailBatchRequest { Emails = [Minimal()] });

        diagnostics.EmailResults.Should().BeEmpty();
    }

    [Fact]
    public async Task A_failed_send_reports_nothing()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("email/send", HttpStatusCode.BadRequest, Fixture.Read("Transport/error-nested-permission.json"));
        RecordingDiagnostics diagnostics = new();
        Smtp2GoClient client = TestClient.Create(handler, diagnostics: diagnostics);

        Func<Task> act = () => client.Email.SendAsync(Minimal());

        await act.Should().ThrowAsync<Smtp2GoApiException>();
        diagnostics.EmailResults.Should().BeEmpty();
    }
}
