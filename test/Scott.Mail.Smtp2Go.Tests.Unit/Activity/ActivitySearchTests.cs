using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Tests.Unit.Email;
using Scott.Mail.Smtp2Go.Tests.Unit.Transport;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.ActivitySearch;

public class ActivitySearchTests
{
    [Fact]
    public void Activity_endpoint_is_seeded_with_its_rate_limit_class()
    {
        EndpointTable.Get("activity/search").Should().Be(new Endpoint("activity/search", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: false, RateLimitClass.ActivitySearch, Endpoint.DefaultMaxBodyBytes));
    }

    [Fact]
    public void Request_serialises_every_documented_field()
    {
        ActivitySearchRequest request = new()
        {
            StartDate = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2026, 9, 11, 0, 0, 0, TimeSpan.Zero),
            Search = "invoice | receipt",
            SearchEmailId = "1u0SwL-B9zBpi9ffUq-JAB2",
            SearchSubject = "Booking",
            SearchSender = "no-reply@example.com",
            SearchRecipient = "someone@example.com",
            SearchUsernames = ["smtpuser", "api-5BFDE1E62529"],
            Subaccounts = ["sub-1"],
            Limit = 1000,
            ContinueToken = "eyJwYWdlIjoyfQ==",
            OnlyLatest = true,
            OnlyLatestBySent = false,
            EventTypes = [ActivityEventType.Processed, ActivityEventType.SoftBounced, ActivityEventType.HardBounced, ActivityEventType.Rejected, ActivityEventType.Spam, ActivityEventType.Delivered, ActivityEventType.Unsubscribed, ActivityEventType.Resubscribed, ActivityEventType.Opened, ActivityEventType.Clicked],
            IncludeHeaders = true,
            CustomHeaders = ["X-MyCustomID"],
            Region = "eu",
        };

        Golden.AssertMatchesFixture(JsonSerializer.Serialize(request, Smtp2GoJsonContext.Default.ActivitySearchRequest), "Activity/search-request-full.json");
    }

    [Fact]
    public void Docs_response_deserialises_with_an_empty_extra()
    {
        ApiResponse<ActivitySearchResult> response = JsonSerializer.Deserialize(Fixture.Read("Activity/search-response.json"), Smtp2GoJsonContext.Default.ApiResponseActivitySearchResult)!;

        response.RequestId.Should().Be("4b661d88-6b2d-11eb-8bb3-f23c92bb31d2");
        response.Data.TotalEvents.Should().Be(1);
        response.Data.ContinueToken.Should().BeNull();
        ActivityEvent evt = response.Data.Events.Should().ContainSingle().Which;
        evt.From.Should().Be("rob@example.co.uk");
        evt.Recipient.Should().Be("jo@another_example.com");
        evt.SubaccountName.Should().Be("Master account");
        evt.EmailId.Should().Be("1u0SwL-B9zBpi9ffUq-JAB2");
        evt.Date.Should().Be(new DateTimeOffset(2022, 11, 12, 7, 44, 58, TimeSpan.Zero));
        evt.EventRaw.Should().Be("delivered");
        evt.Event.Should().Be(ActivityEventType.Delivered);
        evt.Subject.Should().Be("My Test Email");
        evt.Username.Should().Be("api-5BFDE1E62529");
        evt.Sender.Should().Be("rob@example.co.uk");
        evt.To.Should().Be("jo@another_example.com");
        evt.Bcc.Should().Be("audit@example.co.uk");
        evt.SmtpResponse.Should().Be("250 Message received");
        evt.Host.Should().Be("136.143.191.44");
        evt.Headers.Should().StartWith("Content-Type: text/html");
        evt.CustomHeaders.Should().Equal(new Dictionary<string, string> { ["X-MyCustomID"] = "01HMSACEHXHDG4X1CZV89SQMP7" });
        evt.Extra.Should().BeNull();
        response.Data.Extra.Should().BeNull();
    }

    [Fact]
    public void Full_response_maps_every_schema_property_and_unknown_events_stay_readable()
    {
        ApiResponse<ActivitySearchResult> response = JsonSerializer.Deserialize(Fixture.Read("Activity/search-response-full.json"), Smtp2GoJsonContext.Default.ApiResponseActivitySearchResult)!;

        response.Data.ContinueToken.Should().Be("eyJwYWdlIjoyfQ==");
        response.Data.TotalEvents.Should().Be(23405);
        ActivityEvent processed = response.Data.Events![0];
        processed.Event.Should().Be(ActivityEventType.Processed);
        processed.Recipients.Should().Equal("someone@example.com", "someoneelse@example.com");
        processed.ReplyTo.Should().Be("reply@example.com");
        processed.SenderFull.Should().Be("NoReply <no-reply@example.com>");
        processed.Cc.Should().Be("cc@example.com");
        processed.Reason.Should().Be("This was a spam email");
        processed.OriginatingHost.Should().Be("127.0.0.2");
        processed.Error.Should().Be("i/o timeout");
        processed.EmailClient!.Value.GetProperty("name").GetString().Should().Be("Gmail");
        processed.Metadata!.Value.GetProperty("link").GetString().Should().Be("https://example.com");
        processed.OutboundIp.Should().Be("203.0.113.5");
        processed.ByteSize.Should().Be(1422);
        processed.DeliveryAttempts.Should().HaveCount(2);
        processed.DeliveryAttempts![0].SmtpTime.Should().Be(new DateTimeOffset(2021, 2, 9, 12, 18, 54, TimeSpan.Zero));
        processed.DeliveryAttempts[0].Host.Should().Be("mx.example.com");
        processed.DeliveryAttempts[1].SmtpResponse.Should().Be("250 OK");
        processed.DeliveryAttempts.Should().OnlyContain(a => a.Extra == null);
        response.Data.Events[1].Event.Should().Be(ActivityEventType.SoftBounced);
        response.Data.Events[2].Event.Should().Be(ActivityEventType.HardBounced);
        response.Data.Events[3].Event.Should().Be(ActivityEventType.Unknown);
        response.Data.Events[3].EventRaw.Should().Be("quarantined");
        response.Data.Events.Should().OnlyContain(e => e.Extra == null);
    }

    [Fact]
    public void Limit_and_unknown_event_types_are_validated()
    {
        List<string> errors = [];

        ((IRequestValidator)new ActivitySearchRequest { Limit = 1001, EventTypes = [ActivityEventType.Unknown] }).Validate(EndpointTable.Get("activity/search"), errors);

        errors.Should().Equal("limit must be between 1 and 1000.", "event_types must not contain ActivityEventType.Unknown.");
    }

    [Fact]
    public async Task SearchAsync_posts_the_request_and_ignores_subaccount_id()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("activity/search", HttpStatusCode.OK, Fixture.Read("Activity/search-response.json"));
        Smtp2GoClient client = TestClient.Create(handler);

        ApiResponse<ActivitySearchResult> response = await client.Activity.SearchAsync(new ActivitySearchRequest { SearchSubject = "Booking" }, new RequestOptions { SubaccountId = "sub-1" });

        handler.LastRequest.Endpoint!.Path.Should().Be("activity/search");
        handler.LastRequest.Body.Should().Be("""{"search_subject":"Booking"}""", because: "activity/search has no subaccount_id field");
        response.Data.Events.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAllAsync_follows_continue_tokens_and_stops_on_null()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("activity/search", async (request, ct) =>
        {
            JsonNode body = JsonNode.Parse(await request.Content!.ReadAsStringAsync(ct))!;
            string? token = body["continue_token"]?.GetValue<string>();
            (string items, string next) = token switch
            {
                null => ("""{"email_id":"e1"},{"email_id":"e2"}""", "\"t2\""),
                "t2" => ("""{"email_id":"e3"}""", "\"t3\""),
                _ => ("""{"email_id":"e4"}""", "null"),
            };
            return FakeHttpMessageHandler.Json(HttpStatusCode.OK, $$"""{"request_id":"r","data":{"events":[{{items}}],"total_events":4,"continue_token":{{next}} } }""");
        });
        Smtp2GoClient client = TestClient.Create(handler);

        List<string?> ids = [];
        await foreach (ActivityEvent evt in client.Activity.SearchAllAsync(new ActivitySearchRequest { SearchSubject = "x", Limit = 2 }, cancellationToken: TestContext.Current.CancellationToken))
        {
            ids.Add(evt.EmailId);
        }

        ids.Should().Equal("e1", "e2", "e3", "e4");
        handler.Requests.Should().HaveCount(3);
        handler.Requests.Select(r => JsonNode.Parse(r.Body!)!["continue_token"]?.GetValue<string>()).Should().Equal(null, "t2", "t3");
        handler.Requests.Should().OnlyContain(r => r.Body!.Contains("\"search_subject\":\"x\"") && r.Body.Contains("\"limit\":2"));
    }

    [Fact]
    public async Task SearchAllAsync_stops_on_an_empty_or_repeated_token()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("activity/search", HttpStatusCode.OK, """{"request_id":"r","data":{"events":[{"email_id":"e1"}],"continue_token":""}}""");
        Smtp2GoClient client = TestClient.Create(handler);
        List<ActivityEvent> events = [];
        await foreach (ActivityEvent evt in client.Activity.SearchAllAsync(new ActivitySearchRequest(), cancellationToken: TestContext.Current.CancellationToken))
        {
            events.Add(evt);
        }

        events.Should().HaveCount(1);
        handler.Requests.Should().HaveCount(1, because: "an empty token ends the walk");

        FakeHttpMessageHandler repeating = new FakeHttpMessageHandler().Respond("activity/search", HttpStatusCode.OK, """{"request_id":"r","data":{"events":[{"email_id":"e1"}],"continue_token":"same"}}""");
        Smtp2GoClient client2 = TestClient.Create(repeating);
        int count = 0;
        await foreach (ActivityEvent _ in client2.Activity.SearchAllAsync(new ActivitySearchRequest { ContinueToken = "same" }, cancellationToken: TestContext.Current.CancellationToken))
        {
            count++;
        }

        count.Should().Be(1);
        repeating.Requests.Should().HaveCount(1, because: "a token equal to the one just sent would loop forever");
    }

    [Fact]
    public async Task Calls_are_throttled_to_sixty_per_minute()
    {
        FakeTimeProvider clock = new();
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("activity/search", HttpStatusCode.OK, """{"request_id":"r","data":{"events":[],"continue_token":null}}""");
        Smtp2GoConnection connection = new(new HttpClient(handler), new Smtp2GoClientOptions { ApiKey = TestClient.ApiKey }, null, clock);
        ActivityClient client = new(connection);

        for (int i = 0; i < 61; i++)
        {
            await client.SearchAsync(new ActivitySearchRequest(), cancellationToken: TestContext.Current.CancellationToken);
        }

        handler.Requests.Should().HaveCount(61);
        clock.Delays.Should().ContainSingle(because: "the 61st call within the minute waits for one token");
    }

    [Fact]
    public async Task Throttling_can_be_turned_off()
    {
        FakeTimeProvider clock = new();
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("activity/search", HttpStatusCode.OK, """{"request_id":"r","data":{"events":[],"continue_token":null}}""");
        Smtp2GoConnection connection = new(new HttpClient(handler), new Smtp2GoClientOptions { ApiKey = TestClient.ApiKey, ClientSideRateLimiting = false }, null, clock);
        ActivityClient client = new(connection);

        for (int i = 0; i < 61; i++)
        {
            await client.SearchAsync(new ActivitySearchRequest(), cancellationToken: TestContext.Current.CancellationToken);
        }

        clock.Delays.Should().BeEmpty();
        connection.GetThrottle(RateLimitClass.ActivitySearch).Should().BeNull();
    }

    [Fact]
    public async Task Null_requests_are_rejected()
    {
        Smtp2GoClient client = TestClient.Create(new FakeHttpMessageHandler());

        await ((Func<Task>)(() => client.Activity.SearchAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        ((Action)(() => client.Activity.SearchAllAsync(null!))).Should().Throw<ArgumentNullException>();
    }
}
