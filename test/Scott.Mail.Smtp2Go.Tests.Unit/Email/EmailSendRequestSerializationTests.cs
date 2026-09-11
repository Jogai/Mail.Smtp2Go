using System.Text.Json;
using System.Text.Json.Nodes;
using Scott.Mail.Smtp2Go.Json;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Email;

public class EmailSendRequestSerializationTests
{
    internal static string Serialize(EmailSendRequest request)
    {
        return JsonSerializer.Serialize(request, Smtp2GoJsonContext.Default.EmailSendRequest);
    }

    internal static EmailSendRequest FullRequest()
    {
        using JsonDocument meta = JsonDocument.Parse("""{"source":"json-element"}""");
        return new EmailSendRequest
        {
            Sender = new EmailAddress("alice@example.com", "Smith, Alice"),
            To = ["Bob <bob@example.com>", "carol@example.com"],
            Cc = ["dave@example.com"],
            Bcc = ["erin@example.com"],
            Subject = "Invoice {{ number }}",
            HtmlBody = "<p>See attached <img src=\"cid:logo.png\"></p>",
            TextBody = "See attached.",
            CustomHeaders = [new CustomHeader("X-Campaign", "spring"), new CustomHeader("Reply-To", "support@example.com")],
            Attachments = [Attachment.FromBase64("invoice.pdf", "JVBERi0xLjQK")],
            Inlines = [Attachment.FromBase64("logo.png", "iVBORw0KGgo=")],
            TemplateId = "tpl-123",
            TemplateData = new Dictionary<string, object?>
            {
                ["number"] = "INV-42",
                ["amount"] = 12.5,
                ["paid"] = false,
                ["items"] = new[] { "a", "b" },
                ["customer"] = new JsonObject { ["name"] = "Bob" },
                ["meta"] = meta.RootElement.Clone(),
            },
            Schedule = new DateTimeOffset(2026, 9, 12, 12, 0, 0, TimeSpan.FromHours(2)),
            FastAccept = true,
        };
    }

    [Fact]
    public void Minimal_send_serialises_only_the_fields_set()
    {
        EmailSendRequest request = new()
        {
            Sender = "Alice <alice@example.com>",
            To = ["bob@example.com"],
            Subject = "Hello",
            TextBody = "Plain text.",
        };

        Golden.AssertMatches(Serialize(request), "send-minimal.json");
    }

    [Fact]
    public void Full_send_serialises_every_documented_field_with_its_wire_name()
    {
        string json = Serialize(FullRequest());

        Golden.AssertMatches(json, "send-full.json");
        JsonObject root = JsonNode.Parse(json)!.AsObject();
        root.Select(p => p.Key).Should().Equal(
            "sender", "to", "cc", "bcc", "subject", "html_body", "text_body", "custom_headers", "attachments", "inlines", "template_id", "template_data", "schedule", "fastaccept");
    }

    [Fact]
    public void Template_send_needs_neither_subject_nor_bodies()
    {
        EmailSendRequest request = new()
        {
            Sender = "alice@example.com",
            To = ["bob@example.com"],
            TemplateId = "welcome",
            TemplateData = new Dictionary<string, object?> { ["first_name"] = "Bob" },
        };

        Golden.AssertMatches(Serialize(request), "send-template.json");
    }

    [Fact]
    public void Url_attachment_serialises_without_a_blob()
    {
        EmailSendRequest request = new()
        {
            Sender = "alice@example.com",
            To = ["bob@example.com"],
            Subject = "Brochure",
            HtmlBody = "<p>Attached.</p>",
            Attachments = [Attachment.FromUrl("brochure.pdf", new Uri("https://example.com/files/brochure.pdf"))],
        };

        Golden.AssertMatches(Serialize(request), "send-url-attachment.json");
    }

    [Fact]
    public void Empty_collections_serialise_as_empty_arrays_so_leave_them_null_to_omit_them()
    {
        // The serialiser cannot tell "no cc" from "cc = []"; keep Cc null rather than empty. This documents the behaviour the golden files rely on.
        EmailSendRequest request = new() { Sender = "a@example.com", To = ["b@example.com"], TextBody = "x", Cc = [] };

        Serialize(request).Should().Contain("\"cc\":[]");
    }

    [Fact]
    public void Schedule_is_written_as_iso8601_utc()
    {
        EmailSendRequest request = new()
        {
            Sender = "a@example.com",
            To = ["b@example.com"],
            TextBody = "x",
            Schedule = new DateTimeOffset(2026, 9, 10, 13, 15, 0, TimeSpan.FromHours(12)),
        };

        JsonNode.Parse(Serialize(request))!["schedule"]!.GetValue<string>().Should().Be("2026-09-10T01:15:00Z");
    }

    [Fact]
    public void Request_round_trips_through_the_context()
    {
        EmailSendRequest original = FullRequest();

        EmailSendRequest? back = JsonSerializer.Deserialize(Serialize(original), Smtp2GoJsonContext.Default.EmailSendRequest);

        back.Should().NotBeNull();
        back!.Sender.Should().Be(original.Sender);
        back.Sender.Name.Should().Be("Smith, Alice");
        back.To.Should().Equal(original.To);
        back.To[0].Name.Should().Be("Bob");
        back.Schedule.Should().Be(original.Schedule);
        back.FastAccept.Should().BeTrue();
        back.TemplateData!["number"].Should().BeOfType<JsonElement>().Which.GetString().Should().Be("INV-42");
    }
}
