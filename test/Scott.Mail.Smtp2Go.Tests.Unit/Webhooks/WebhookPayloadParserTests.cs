using System.Text;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Webhooks;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Webhooks;

public class WebhookPayloadParserTests
{
    private static readonly WebhookPayloadParser s_parser = WebhookPayloadParser.Default;

    /// <summary>Expected kind per fixture stem, for both folders; the file name is the wire event name (with <c>-hard</c>/<c>-soft</c> suffixes for bounces).</summary>
    private static readonly Dictionary<string, WebhookEventKind> s_expectedKinds = new(StringComparer.Ordinal)
    {
        ["processed"] = WebhookEventKind.EmailProcessed,
        ["delivered"] = WebhookEventKind.EmailDelivered,
        ["bounce"] = WebhookEventKind.EmailBounce,
        ["bounce-hard"] = WebhookEventKind.EmailBounce,
        ["bounce-soft"] = WebhookEventKind.EmailBounce,
        ["open"] = WebhookEventKind.EmailOpen,
        ["opened"] = WebhookEventKind.EmailOpen,
        ["click"] = WebhookEventKind.EmailClick,
        ["clicked"] = WebhookEventKind.EmailClick,
        ["spam"] = WebhookEventKind.EmailSpam,
        ["spam_complaint"] = WebhookEventKind.EmailSpam,
        ["unsubscribe"] = WebhookEventKind.EmailUnsubscribe,
        ["unsubscribed"] = WebhookEventKind.EmailUnsubscribe,
        ["resubscribe"] = WebhookEventKind.EmailResubscribe,
        ["reject"] = WebhookEventKind.EmailReject,
        ["sms_sending"] = WebhookEventKind.SmsSending,
        ["sms_submitted"] = WebhookEventKind.SmsSubmitted,
        ["sms_delivered"] = WebhookEventKind.SmsDelivered,
        ["sms_failed"] = WebhookEventKind.SmsFailed,
        ["sms_rejected"] = WebhookEventKind.SmsRejected,
        ["unknown_future"] = WebhookEventKind.Unknown,
    };

    public static TheoryData<string, string> AllFixtures()
    {
        TheoryData<string, string> data = [];
        foreach (string folder in new[] { "Docs", "Live" })
        {
            foreach (string path in Directory.GetFiles(Fixture.PathOf("Webhooks/" + folder)).OrderBy(p => p, StringComparer.Ordinal))
            {
                data.Add(folder, Path.GetFileName(path));
            }
        }

        return data;
    }

    private static WebhookEvent ParseFixture(string folder, string file)
    {
        string body = Fixture.Read($"Webhooks/{folder}/{file}");
        return file.EndsWith(".json", StringComparison.Ordinal) ? s_parser.Parse(body) : s_parser.ParseFormBody(body);
    }

    [Theory]
    [MemberData(nameof(AllFixtures))]
    public void Every_fixture_parses_to_the_expected_kind_with_nothing_left_in_extra(string folder, string file)
    {
        string stem = Path.GetFileNameWithoutExtension(file);
        WebhookEvent evt = ParseFixture(folder, file);

        evt.Kind.Should().Be(s_expectedKinds[stem], because: "{0}/{1} is a {2} callback", folder, file, stem);
        evt.EventRaw.Should().NotBeNullOrEmpty();
        if (folder == "Docs")
        {
            evt.WebhookId.Should().NotBeNullOrEmpty(because: "the docs fixtures carry every documented field");
        }

        if (evt.Kind == WebhookEventKind.Unknown)
        {
            evt.Should().BeOfType<UnknownWebhookEvent>();
            evt.Extra.Should().ContainKeys("email_id", "rcpt", "reason");
        }
        else
        {
            evt.Extra.Should().BeNull(because: "every field of {0}/{1} must be modelled; nothing may land in Extra", folder, file);
            evt.Time.Should().NotBeNull();
        }
    }

    [Theory]
    [MemberData(nameof(AllFixtures))]
    public void Every_fixture_has_a_twin_in_the_other_encoding_that_parses_to_the_same_event(string folder, string file)
    {
        string stem = Path.GetFileNameWithoutExtension(file);
        string twin = stem + (file.EndsWith(".json", StringComparison.Ordinal) ? ".form" : ".json");
        File.Exists(Fixture.PathOf($"Webhooks/{folder}/{twin}")).Should().BeTrue(because: "{0}/{1} needs a {2} twin", folder, file, twin);

        WebhookEvent fromThis = ParseFixture(folder, file);
        WebhookEvent fromTwin = ParseFixture(folder, twin);

        fromTwin.GetType().Should().Be(fromThis.GetType());
        fromTwin.Kind.Should().Be(fromThis.Kind);
        if (folder == "Docs")
        {
            // Docs forms are generated from the JSON; Live JSON and form captures were recorded on different days, so only the shape is compared there.
            fromTwin.Should().BeEquivalentTo(fromThis, o => o.Excluding(e => e.Extra), because: "JSON and form encodings of {0}/{1} carry the same data", folder, stem);
        }
    }

    [Fact]
    public void Docs_delivered_maps_every_documented_field()
    {
        EmailDeliveredEvent evt = s_parser.Parse(Fixture.Read("Webhooks/Docs/delivered.json")).Should().BeOfType<EmailDeliveredEvent>().Which;

        evt.Kind.Should().Be(WebhookEventKind.EmailDelivered);
        evt.EventRaw.Should().Be("delivered");
        evt.Time.Should().Be(new DateTimeOffset(2026, 9, 10, 8, 0, 5, TimeSpan.Zero));
        evt.SendTime.Should().Be(new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero));
        evt.Sender.Should().Be("bounce@example.com");
        evt.From.Should().Be("Alice <alice@example.com>");
        evt.FromAddress.Should().Be("alice@example.com");
        evt.FromName.Should().Be("Alice");
        evt.Recipient.Should().Be("bob@example.org");
        evt.Recipients.Should().BeNull();
        evt.Auth.Should().Be("api-5BFDE1E62529");
        evt.Host.Should().Be("mx.example.org [198.51.100.25]");
        evt.Message.Should().Be("250 2.0.0 Ok: queued as 4f7f4b3tWbzKy");
        evt.Context.Should().Be("Unavailable");
        evt.EmailId.Should().Be("1u0SwL-B9zBpi9ffUq-JAB2");
        evt.WebhookId.Should().Be("6dfa7d3b4514c1f5f0e916bc0cc0395c");
        evt.MessageId.Should().Be("<E1u0SwL-B9zBpi9ffUq-JAB2@message-id.smtpcorp.com>");
        evt.Subject.Should().Be("Docs delivered");
        evt.CustomHeaders.Should().BeNull();
        evt.Extra.Should().BeNull();
    }

    [Fact]
    public void Docs_open_maps_the_engagement_and_geo_fields()
    {
        EmailOpenEvent evt = s_parser.Parse(Fixture.Read("Webhooks/Docs/open.json")).Should().BeOfType<EmailOpenEvent>().Which;

        evt.EventRaw.Should().Be("open");
        evt.UserAgent.Should().Be("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X)");
        evt.ReadSeconds.Should().Be(15);
        evt.Client.Should().Be("Apple Mail");
        evt.ClientDevice.Should().Be("Mobile");
        evt.ClientOs.Should().Be("iOS");
        evt.GeoContinent.Should().Be("EU");
        evt.GeoCountry.Should().Be("NL");
        evt.GeoCity.Should().Be("Amsterdam");
        evt.SourceHost.Should().Be("192.0.2.44");
    }

    [Fact]
    public void Docs_click_is_an_open_event_with_the_open_fields()
    {
        WebhookEvent evt = s_parser.ParseFormBody(Fixture.Read("Webhooks/Docs/click.form"));

        EmailClickEvent click = evt.Should().BeOfType<EmailClickEvent>().Which;
        click.Should().BeAssignableTo<EmailOpenEvent>();
        click.EventRaw.Should().Be("click");
        click.Client.Should().Be("Chrome");
        click.GeoCity.Should().Be("Utrecht");
        click.Link.Should().BeNull();
        click.ClickUrl.Should().BeNull();
    }

    [Fact]
    public void Docs_bounce_maps_the_bounce_type_and_server_response()
    {
        EmailBounceEvent evt = s_parser.Parse(Fixture.Read("Webhooks/Docs/bounce.json")).Should().BeOfType<EmailBounceEvent>().Which;

        evt.BounceType.Should().Be(BounceType.Hard);
        evt.BounceRaw.Should().Be("hard");
        evt.Host.Should().Be("mx.example.org [198.51.100.25]");
        evt.Message.Should().StartWith("550 5.1.1");
        evt.Context.Should().Be("RCPT TO:<nobody@example.org>");
        evt.Recipient.Should().Be("nobody@example.org");
    }

    [Fact]
    public void Docs_reject_and_resubscribe_map_to_their_types()
    {
        s_parser.Parse(Fixture.Read("Webhooks/Docs/reject.json")).Should().BeOfType<EmailRejectEvent>().Which.Message.Should().Be("Recipient address is on the suppression list");
        s_parser.Parse(Fixture.Read("Webhooks/Docs/resubscribe.json")).Should().BeOfType<EmailResubscribeEvent>().Which.Recipient.Should().Be("bob@example.org");
        s_parser.Parse(Fixture.Read("Webhooks/Docs/spam.json")).Should().BeOfType<EmailSpamEvent>();
        s_parser.Parse(Fixture.Read("Webhooks/Docs/unsubscribe.json")).Should().BeOfType<EmailUnsubscribeEvent>();
    }

    [Fact]
    public void Docs_sms_event_maps_the_sms_parameter_table()
    {
        SmsStatusEvent evt = s_parser.Parse(Fixture.Read("Webhooks/Docs/sms_delivered.json")).Should().BeOfType<SmsStatusEvent>().Which;

        evt.Kind.Should().Be(WebhookEventKind.SmsDelivered);
        evt.EventRaw.Should().Be("sms_delivered");
        evt.DestinationNumber.Should().Be("+31612345678");
        evt.EmailSubject.Should().Be("Docs SMS");
        evt.WebhookId.Should().Be("72d3b38e9f0a1b2c3d4e5f6071829304");
        evt.MessageContent.Should().Be("Your code is 123456");
        evt.MessageId.Should().Be("sms-9c1f2e3d");
        evt.ReceivedTimestamp.Should().Be(new DateTimeOffset(2026, 9, 10, 10, 0, 0, TimeSpan.Zero));
        evt.Time.Should().Be(evt.ReceivedTimestamp, because: "SMS callbacks have no time field");
        evt.Region.Should().Be("NL");
        evt.RetryCount.Should().Be(1);
        evt.SenderEmail.Should().Be("alerts@example.com");
        evt.SourceNumber.Should().Be("+3197010000000");
        evt.StatusCode.Should().Be("delivered");
        evt.SubmittedTimestamp.Should().Be(new DateTimeOffset(2026, 9, 10, 10, 0, 1, TimeSpan.Zero));
    }

    [Fact]
    public void Live_delivered_form_merges_the_mixed_case_duplicate_keys()
    {
        EmailDeliveredEvent evt = s_parser.ParseFormBody(Fixture.Read("Webhooks/Live/delivered.form")).Should().BeOfType<EmailDeliveredEvent>().Which;

        evt.MessageId.Should().Be("<E1w4x2g-FnQW0hPru7M-NRRC@message-id.smtpcorp.com>");
        evt.Subject.Should().Be("Webhook Delivery Test - b83d60289ef94e028a45a905198ad9b7");
        evt.EmailId.Should().Be("1w4x2g-FnQW0hPru7M-NRRC");
        evt.WebhookId.Should().Be("6dfa7d3b4514c1f5f0e916bc0cc0395c");
        evt.Auth.Should().Be("api-597435AE4E55");
        evt.Recipient.Should().Be("alexis.pujo@pm.me");
        evt.Recipients.Should().BeNull();
        evt.Sender.Should().Be("testing@dev.mjosdrone.no");
        evt.FromName.Should().BeEmpty();
        evt.Time.Should().Be(new DateTimeOffset(2026, 3, 24, 8, 23, 19, TimeSpan.Zero));
        evt.SendTime.Should().Be(new DateTimeOffset(2026, 3, 24, 8, 23, 19, 52, TimeSpan.Zero).AddTicks(7650));
        evt.Host.Should().Be("mail.protonmail.ch [185.205.70.128]");
        evt.Context.Should().Be("Unavailable");
        evt.Message.Should().Be("250 2.0.0 Ok: 2780 bytes queued as 4fg32Y2xRcz3T");
        evt.Extra.Should().BeNull();
    }

    [Fact]
    public void Live_processed_json_has_recipients_but_no_rcpt()
    {
        EmailProcessedEvent evt = s_parser.Parse(Fixture.Read("Webhooks/Live/processed.json")).Should().BeOfType<EmailProcessedEvent>().Which;

        evt.Recipient.Should().BeNull();
        evt.Recipients.Should().Equal("user@example.com", "user2@example.com");
        evt.SourceHost.Should().Be("146.70.170.30");
        evt.SendTime.Should().Be(new DateTimeOffset(2026, 2, 7, 18, 5, 2, 199, TimeSpan.Zero).AddTicks(3240));
    }

    [Fact]
    public void Live_clicked_maps_link_and_click_url()
    {
        EmailClickEvent evt = s_parser.Parse(Fixture.Read("Webhooks/Live/clicked.json")).Should().BeOfType<EmailClickEvent>().Which;

        evt.EventRaw.Should().Be("clicked");
        evt.Kind.Should().Be(WebhookEventKind.EmailClick);
        evt.ClickUrl.Should().Be("https://alos.app/dashboard");
        evt.Link.Should().Be("https://track.smtp2go.com/abc123");
        evt.Recipient.Should().Be("user@example.com");
    }

    [Theory]
    [InlineData("Live/bounce-hard.json", BounceType.Hard, "hard")]
    [InlineData("Live/bounce-soft.form", BounceType.Soft, "soft")]
    public void Live_bounces_classify_hard_and_soft(string fixture, BounceType expected, string raw)
    {
        string body = Fixture.Read("Webhooks/" + fixture);
        EmailBounceEvent evt = (fixture.EndsWith(".json", StringComparison.Ordinal) ? s_parser.Parse(body) : s_parser.ParseFormBody(body)).Should().BeOfType<EmailBounceEvent>().Which;

        evt.BounceType.Should().Be(expected);
        evt.BounceRaw.Should().Be(raw);
        evt.Context.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("processed", WebhookEventKind.EmailProcessed, typeof(EmailProcessedEvent))]
    [InlineData("delivered", WebhookEventKind.EmailDelivered, typeof(EmailDeliveredEvent))]
    [InlineData("bounce", WebhookEventKind.EmailBounce, typeof(EmailBounceEvent))]
    [InlineData("open", WebhookEventKind.EmailOpen, typeof(EmailOpenEvent))]
    [InlineData("opened", WebhookEventKind.EmailOpen, typeof(EmailOpenEvent))]
    [InlineData("Opened", WebhookEventKind.EmailOpen, typeof(EmailOpenEvent))]
    [InlineData("click", WebhookEventKind.EmailClick, typeof(EmailClickEvent))]
    [InlineData("clicked", WebhookEventKind.EmailClick, typeof(EmailClickEvent))]
    [InlineData("spam", WebhookEventKind.EmailSpam, typeof(EmailSpamEvent))]
    [InlineData("spam_complaint", WebhookEventKind.EmailSpam, typeof(EmailSpamEvent))]
    [InlineData("unsubscribe", WebhookEventKind.EmailUnsubscribe, typeof(EmailUnsubscribeEvent))]
    [InlineData("unsubscribed", WebhookEventKind.EmailUnsubscribe, typeof(EmailUnsubscribeEvent))]
    [InlineData("resubscribe", WebhookEventKind.EmailResubscribe, typeof(EmailResubscribeEvent))]
    [InlineData("resubscribed", WebhookEventKind.EmailResubscribe, typeof(EmailResubscribeEvent))]
    [InlineData("reject", WebhookEventKind.EmailReject, typeof(EmailRejectEvent))]
    [InlineData("rejected", WebhookEventKind.EmailReject, typeof(EmailRejectEvent))]
    [InlineData("sms_sending", WebhookEventKind.SmsSending, typeof(SmsStatusEvent))]
    [InlineData("sms_submitted", WebhookEventKind.SmsSubmitted, typeof(SmsStatusEvent))]
    [InlineData("sms_delivered", WebhookEventKind.SmsDelivered, typeof(SmsStatusEvent))]
    [InlineData("sms_failed", WebhookEventKind.SmsFailed, typeof(SmsStatusEvent))]
    [InlineData("sms_rejected", WebhookEventKind.SmsRejected, typeof(SmsStatusEvent))]
    [InlineData("sms_opt_out", WebhookEventKind.SmsOptOut, typeof(SmsStatusEvent))]
    [InlineData("some_future_event", WebhookEventKind.Unknown, typeof(UnknownWebhookEvent))]
    [InlineData("hard_bounced", WebhookEventKind.Unknown, typeof(UnknownWebhookEvent))]
    [InlineData("soft_bounced", WebhookEventKind.Unknown, typeof(UnknownWebhookEvent))]
    public void Event_names_from_the_docs_and_from_live_callbacks_resolve_the_same_way_in_json_and_form(string wire, WebhookEventKind kind, Type type)
    {
        WebhookEvent fromJson = s_parser.Parse($$"""{"event":"{{wire}}"}""");
        WebhookEvent fromForm = s_parser.ParseForm([new KeyValuePair<string, string?>("event", wire)]);

        fromJson.Kind.Should().Be(kind);
        fromJson.Should().BeOfType(type);
        fromJson.EventRaw.Should().Be(wire, because: "the wire value is retained verbatim");
        fromForm.Kind.Should().Be(kind);
        fromForm.Should().BeOfType(type);
        fromForm.EventRaw.Should().Be(wire);
    }

    [Fact]
    public void Unknown_event_keeps_the_full_bag_in_extra()
    {
        WebhookEvent evt = s_parser.Parse("""{"event":"quarantined","time":"2026-09-10T08:00:09Z","id":"w1","email_id":"e1","score":7.5,"tags":["a","b"]}""");

        UnknownWebhookEvent unknown = evt.Should().BeOfType<UnknownWebhookEvent>().Which;
        unknown.EventRaw.Should().Be("quarantined");
        unknown.WebhookId.Should().Be("w1");
        unknown.Time.Should().Be(new DateTimeOffset(2026, 9, 10, 8, 0, 9, TimeSpan.Zero));
        unknown.Extra.Should().ContainKeys("email_id", "score", "tags");
        unknown.Extra!["score"].GetDouble().Should().Be(7.5);
        unknown.Extra["tags"].ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public void Missing_event_yields_unknown_with_an_empty_raw_value()
    {
        WebhookEvent evt = s_parser.Parse("""{"time":"2026-09-10T08:00:09Z"}""");

        evt.Kind.Should().Be(WebhookEventKind.Unknown);
        evt.EventRaw.Should().BeEmpty();
    }

    [Fact]
    public void Declared_custom_headers_are_lifted_out_of_extra_and_undeclared_ones_stay()
    {
        WebhookPayloadParser parser = new(new WebhookParserOptions { KnownCustomHeaders = ["X-Campaign", "X-Customer-Id"] });

        EmailDeliveredEvent evt = parser.Parse("""{"event":"delivered","rcpt":"bob@example.org","X-Campaign":"spring","x_customer_id":"42","X-Other":"kept"}""").Should().BeOfType<EmailDeliveredEvent>().Which;

        evt.CustomHeaders.Should().BeEquivalentTo(new Dictionary<string, string> { ["X-Campaign"] = "spring", ["X-Customer-Id"] = "42" });
        evt.Extra.Should().ContainSingle().Which.Key.Should().Be("X-Other");
        evt.Extra!["X-Other"].GetString().Should().Be("kept");
    }

    [Fact]
    public void Custom_headers_work_for_form_input_too()
    {
        WebhookPayloadParser parser = new(new WebhookParserOptions { KnownCustomHeaders = ["X-Campaign"] });

        EmailOpenEvent evt = parser.ParseFormBody("event=opened&rcpt=bob%40example.org&X-Campaign=spring&extra=1").Should().BeOfType<EmailOpenEvent>().Which;

        evt.CustomHeaders.Should().Equal(new Dictionary<string, string> { ["X-Campaign"] = "spring" });
        evt.Extra!["extra"].GetString().Should().Be("1");
    }

    [Fact]
    public void Form_recipients_repeated_and_delimited_are_normalised()
    {
        WebhookEvent evt = s_parser.ParseForm(
        [
            new("event", "processed"),
            new("recipients", "one@example.com"),
            new("recipients", "two@example.com; three@example.com"),
            new("recipients", "four@example.com,five@example.com"),
        ]);

        evt.Should().BeOfType<EmailProcessedEvent>().Which.Recipients.Should().Equal("one@example.com", "two@example.com", "three@example.com", "four@example.com", "five@example.com");
    }

    [Fact]
    public void Json_recipients_given_as_one_delimited_string_are_split_as_well()
    {
        WebhookEvent evt = s_parser.Parse("""{"event":"processed","recipients":"a@example.com, b@example.com"}""");

        evt.Should().BeOfType<EmailProcessedEvent>().Which.Recipients.Should().Equal("a@example.com", "b@example.com");
    }

    [Fact]
    public void Invalid_timestamps_and_numbers_become_null_rather_than_throwing()
    {
        EmailOpenEvent evt = s_parser.ParseForm([new("event", "opened"), new("time", "not-a-timestamp"), new("read-secs", "lots")]).Should().BeOfType<EmailOpenEvent>().Which;

        evt.Time.Should().BeNull();
        evt.ReadSeconds.Should().BeNull();
    }

    [Fact]
    public void Form_body_decoding_handles_plus_and_percent_escapes()
    {
        EmailDeliveredEvent evt = s_parser.ParseFormBody("event=delivered&subject=Hello+World+%26+friends&rcpt=bob%40example.org").Should().BeOfType<EmailDeliveredEvent>().Which;

        evt.Subject.Should().Be("Hello World & friends");
        evt.Recipient.Should().Be("bob@example.org");
    }

    [Theory]
    [InlineData("application/json", "{\"event\":\"delivered\"}", typeof(EmailDeliveredEvent))]
    [InlineData("application/json; charset=utf-8", "{\"event\":\"delivered\"}", typeof(EmailDeliveredEvent))]
    [InlineData("application/x-www-form-urlencoded", "event=opened", typeof(EmailOpenEvent))]
    [InlineData(null, "  {\"event\":\"bounce\"}", typeof(EmailBounceEvent))]
    [InlineData(null, "event=clicked", typeof(EmailClickEvent))]
    public void Parse_with_content_type_dispatches_on_media_type_or_sniffs(string? contentType, string body, Type expected)
    {
        s_parser.Parse(body, contentType).Should().BeOfType(expected);
    }

    [Fact]
    public async Task Stream_and_span_overloads_match_the_string_overload()
    {
        string json = Fixture.Read("Webhooks/Live/delivered.json");
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        WebhookEvent fromString = s_parser.Parse(json);
        WebhookEvent fromSpan = s_parser.Parse(bytes.AsSpan());
        using MemoryStream stream = new(bytes);
        WebhookEvent fromStream = await s_parser.ParseAsync(stream, TestContext.Current.CancellationToken);

        fromSpan.Should().BeEquivalentTo(fromString, o => o.Excluding(e => e.Extra));
        fromStream.Should().BeEquivalentTo(fromString, o => o.Excluding(e => e.Extra));
    }

    [Fact]
    public void Non_object_json_is_rejected()
    {
        Action act = () => s_parser.Parse("[1,2]");

        act.Should().Throw<JsonException>().WithMessage("*must be a JSON object*");
    }

    [Fact]
    public void Null_arguments_are_rejected()
    {
        ((Action)(() => s_parser.Parse((string)null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => s_parser.ParseForm(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => s_parser.ParseFormBody(null!))).Should().Throw<ArgumentNullException>();
    }
}
