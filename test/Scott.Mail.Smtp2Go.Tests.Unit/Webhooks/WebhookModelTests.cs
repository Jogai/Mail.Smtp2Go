using System.Text.Json;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Tests.Unit.Email;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Webhooks;

public class WebhookModelTests
{
    public static readonly WebhookAddRequest FullAddRequest = new()
    {
        Url = "https://example.com/hooks/smtp2go",
        Events = [WebhookEmailEvent.Processed, WebhookEmailEvent.Delivered, WebhookEmailEvent.Bounce, WebhookEmailEvent.Open, WebhookEmailEvent.Click, WebhookEmailEvent.Spam, WebhookEmailEvent.Unsubscribe, WebhookEmailEvent.Resubscribe, WebhookEmailEvent.Reject],
        SmsEvents = [WebhookSmsEvent.Sending, WebhookSmsEvent.Submitted, WebhookSmsEvent.Delivered, WebhookSmsEvent.Failed, WebhookSmsEvent.Rejected, WebhookSmsEvent.OptOut],
        Headers = ["X-Campaign", "X-Customer-Id"],
        Usernames = ["api-5BFDE1E62529", "smtpuser"],
        OutputFormat = WebhookOutputFormat.Json,
        AuthHeaderType = WebhookAuthHeaderType.Bearer,
        AuthHeaderValue = "s3cr3t-token",
    };

    [Fact]
    public void Add_request_serialises_every_documented_field()
    {
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(FullAddRequest, Smtp2GoJsonContext.Default.WebhookAddRequest), "Webhooks/add-request-full.json");
    }

    [Fact]
    public void Minimal_add_request_serialises_only_the_url()
    {
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(new WebhookAddRequest { Url = "https://example.com/hooks/smtp2go" }, Smtp2GoJsonContext.Default.WebhookAddRequest), "Webhooks/add-request-minimal.json");
    }

    [Fact]
    public void Output_format_uses_the_documented_wire_name_not_output()
    {
        string json = JsonSerializer.Serialize(new WebhookAddRequest { Url = "https://example.com/x", OutputFormat = WebhookOutputFormat.Json }, Smtp2GoJsonContext.Default.WebhookAddRequest);

        json.Should().Contain("\"output_format\":\"json\"").And.NotContain("\"output\":");
    }

    [Theory]
    [InlineData(WebhookAuthHeaderType.Basic, "basic")]
    [InlineData(WebhookAuthHeaderType.Bearer, "bearer")]
    [InlineData(WebhookAuthHeaderType.None, "")]
    public void Auth_header_type_serialises_lower_case(WebhookAuthHeaderType type, string expected)
    {
        string json = JsonSerializer.Serialize(new WebhookEditRequest { Id = 1, AuthHeaderType = type }, Smtp2GoJsonContext.Default.WebhookEditRequest);

        json.Should().Be($$"""{"id":1,"auth_header_type":"{{expected}}"}""");
    }

    [Fact]
    public void Edit_request_serialises_id_and_changed_fields_only()
    {
        WebhookEditRequest request = new()
        {
            Id = 4320,
            Url = "https://example.com/hooks/smtp2go-v2",
            Events = [WebhookEmailEvent.Processed, WebhookEmailEvent.Open, WebhookEmailEvent.Spam],
            OutputFormat = WebhookOutputFormat.Form,
            AuthHeaderType = WebhookAuthHeaderType.Basic,
            AuthHeaderValue = "dXNlcjpwYXNz",
        };

        Golden.AssertMatchesFixture(JsonSerializer.Serialize(request, Smtp2GoJsonContext.Default.WebhookEditRequest), "Webhooks/edit-request.json");
    }

    [Fact]
    public void Edit_request_from_add_request_copies_every_field()
    {
        WebhookEditRequest edit = WebhookEditRequest.From(4320, FullAddRequest);

        edit.Id.Should().Be(4320);
        edit.Should().BeEquivalentTo(FullAddRequest, o => o.ExcludingMissingMembers());
    }

    [Fact]
    public void Clearing_the_auth_header_sends_the_documented_empty_string()
    {
        string json = JsonSerializer.Serialize(new WebhookEditRequest { Id = 4320, AuthHeaderType = WebhookAuthHeaderType.None }, Smtp2GoJsonContext.Default.WebhookEditRequest);

        JsonSerializer.Serialize(System.Text.Json.Nodes.JsonNode.Parse(json)).Should().Be(JsonSerializer.Serialize(System.Text.Json.Nodes.JsonNode.Parse(Fixture.Read("Webhooks/edit-request-clear-auth.json"))));
    }

    [Fact]
    public void Remove_request_serialises_the_id()
    {
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(new WebhookRemoveRequest { Id = 4317 }, Smtp2GoJsonContext.Default.WebhookRemoveRequest), "Webhooks/remove-request.json");
    }

    [Fact]
    public void Docs_add_response_deserialises_including_string_valued_headers_and_usernames()
    {
        ApiResponse<Webhook> response = JsonSerializer.Deserialize(Fixture.Read("Webhooks/add-response.json"), Smtp2GoJsonContext.Default.ApiResponseWebhook)!;

        Webhook webhook = response.Data;
        webhook.Id.Should().Be(4320);
        webhook.Url.Should().Be("https://example.com/test-webhook");
        webhook.Events.Should().Equal(WebhookEmailEvent.Processed);
        webhook.SmsEvents.Should().Equal(WebhookSmsEvent.Sending);
        webhook.Headers.Should().Equal("X-Test-Header");
        webhook.Usernames.Should().Equal("MyUser1");
        webhook.OutputFormat.Should().Be(WebhookOutputFormat.Json);
        webhook.AuthHeaderType.Should().BeNull();
        webhook.Extra.Should().BeNull();
    }

    [Fact]
    public void Docs_edit_response_deserialises()
    {
        ApiResponse<Webhook> response = JsonSerializer.Deserialize(Fixture.Read("Webhooks/edit-response.json"), Smtp2GoJsonContext.Default.ApiResponseWebhook)!;

        response.Data.Events.Should().Equal(WebhookEmailEvent.Processed, WebhookEmailEvent.Open, WebhookEmailEvent.Spam);
        response.Data.SmsEvents.Should().BeEmpty();
        response.Data.OutputFormat.Should().Be(WebhookOutputFormat.Form);
        response.Data.Extra.Should().BeNull();
    }

    [Fact]
    public void View_response_with_an_object_becomes_a_one_item_list()
    {
        ApiResponse<IReadOnlyList<Webhook>> response = JsonSerializer.Deserialize(Fixture.Read("Webhooks/view-response-object.json"), Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListWebhook)!;

        Webhook webhook = response.Data.Should().ContainSingle().Which;
        webhook.Id.Should().Be(4317);
        webhook.Events.Should().Equal(WebhookEmailEvent.Spam);
        webhook.Headers.Should().BeEmpty();
        webhook.Extra.Should().BeNull();
    }

    [Fact]
    public void View_response_with_an_array_is_read_as_is()
    {
        ApiResponse<IReadOnlyList<Webhook>> response = JsonSerializer.Deserialize(Fixture.Read("Webhooks/view-response-array.json"), Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListWebhook)!;

        response.Data.Should().HaveCount(2);
        response.Data[0].AuthHeaderType.Should().Be(WebhookAuthHeaderType.Basic);
        response.Data[0].AuthHeaderValue.Should().Be("dXNlcjpwYXNz");
        response.Data[1].AuthHeaderType.Should().Be(WebhookAuthHeaderType.None);
        response.Data[1].Events.Should().Equal(WebhookEmailEvent.Processed, WebhookEmailEvent.Delivered, WebhookEmailEvent.Bounce);
        response.Data[1].Headers.Should().Equal("X-Campaign");
        response.Data.Should().OnlyContain(w => w.Extra == null);
    }

    [Fact]
    public void Webhook_list_round_trips_as_an_array()
    {
        ApiResponse<IReadOnlyList<Webhook>> response = JsonSerializer.Deserialize(Fixture.Read("Webhooks/view-response-object.json"), Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListWebhook)!;

        string json = JsonSerializer.Serialize(response, Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListWebhook);

        json.Should().StartWith("""{"request_id":"f3e50113-deb2-4e54-9675-2ea497c3732e","data":[{""");
    }

    [Fact]
    public void Unknown_event_names_in_responses_map_to_unknown()
    {
        ApiResponse<Webhook> response = JsonSerializer.Deserialize("""{"request_id":"r","data":{"id":1,"events":["processed","brand_new"],"sms_events":["opt_out"],"output_format":"xml"}}""", Smtp2GoJsonContext.Default.ApiResponseWebhook)!;

        response.Data.Events.Should().Equal(WebhookEmailEvent.Processed, WebhookEmailEvent.Unknown);
        response.Data.SmsEvents.Should().Equal(WebhookSmsEvent.OptOut);
        response.Data.OutputFormat.Should().Be(WebhookOutputFormat.Unknown);
    }

    [Theory]
    [InlineData("ftp://example.com/x", "url must be an absolute http or https URL.")]
    [InlineData("not a url", "url must be an absolute http or https URL.")]
    public void Add_request_rejects_bad_urls(string url, string expected)
    {
        List<string> errors = [];

        ((IRequestValidator)new WebhookAddRequest { Url = url }).Validate(EndpointTable.Get("webhook/add"), errors);

        errors.Should().Equal(expected);
    }

    [Fact]
    public void Add_request_rejects_unknown_enum_members_and_missing_auth_value()
    {
        List<string> errors = [];
        WebhookAddRequest request = new()
        {
            Url = "https://example.com/x",
            Events = [WebhookEmailEvent.Unknown],
            SmsEvents = [WebhookSmsEvent.Unknown],
            OutputFormat = WebhookOutputFormat.Unknown,
            AuthHeaderType = WebhookAuthHeaderType.Bearer,
        };

        ((IRequestValidator)request).Validate(EndpointTable.Get("webhook/add"), errors);

        errors.Should().Equal(
            "events must not contain WebhookEmailEvent.Unknown.",
            "sms_events must not contain WebhookSmsEvent.Unknown.",
            "output_format must not be WebhookOutputFormat.Unknown.",
            "auth_header_value is required when auth_header_type is basic or bearer.");
    }

    [Fact]
    public void Edit_request_rejects_a_non_positive_id()
    {
        List<string> errors = [];

        ((IRequestValidator)new WebhookEditRequest { Id = 0 }).Validate(EndpointTable.Get("webhook/edit"), errors);

        errors.Should().Equal("id must be a positive webhook id.");
    }
}
