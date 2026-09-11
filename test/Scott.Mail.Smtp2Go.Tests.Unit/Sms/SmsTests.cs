using System.Net;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Tests.Unit.Email;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Sms;

public class SmsTests
{
    [Fact]
    public void Sms_family_is_seeded()
    {
        IEnumerable<Endpoint> family = EndpointTable.All.Where(e => e.Path.StartsWith("sms/", StringComparison.Ordinal));

        family.Select(e => e.Path).Should().BeEquivalentTo("sms/send", "sms/summary", "sms/view-received", "sms/view-sent");
        family.Should().OnlyContain(e => e.Method == HttpMethod.Post && e.RateLimit == RateLimitClass.None);
        family.Where(e => !e.AcceptsSubaccountId).Select(e => e.Path).Should().Equal("sms/send");
        family.Where(e => !e.Idempotent).Select(e => e.Path).Should().Equal("sms/send");
    }

    [Fact]
    public void Requests_serialise_the_documented_fields()
    {
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(new SmsSendRequest { Destination = ["+12025550959", "12025550960"], Sender = "+12025550100", Content = "Your code is 123456" }, Smtp2GoJsonContext.Default.SmsSendRequest), "Sms/send-request.json");
#pragma warning disable CS0618 // The deprecated unix_start/unix_end fields must still serialise for callers that use them.
        SmsReceivedRequest received = new()
        {
            StartDate = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2026, 9, 8, 0, 0, 0, TimeSpan.Zero),
            UnixStart = 1788220800,
            UnixEnd = 1788825600,
            Username = "api-12345678",
        };
#pragma warning restore CS0618
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(received, Smtp2GoJsonContext.Default.SmsReceivedRequest), "Sms/view-received-request.json");
        JsonSerializer.Serialize(new SmsSummaryRequest { StartDate = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero) }, Smtp2GoJsonContext.Default.SmsSummaryRequest).Should().Be("""{"start_date":"2026-09-01T00:00:00Z"}""");
        JsonSerializer.Serialize(new SmsSentRequest { Username = "u" }, Smtp2GoJsonContext.Default.SmsSentRequest).Should().Be("""{"username":"u"}""");
        JsonSerializer.Serialize(new SmsSentRequest(), Smtp2GoJsonContext.Default.SmsSentRequest).Should().Be("{}");
    }

    [Fact]
    public void Docs_responses_deserialise_with_an_empty_extra()
    {
        ApiResponse<SmsSendResult> sent = JsonSerializer.Deserialize(Fixture.Read("Sms/send-response.json"), Smtp2GoJsonContext.Default.ApiResponseSmsSendResult)!;
        ApiResponse<SmsSummary> summary = JsonSerializer.Deserialize(Fixture.Read("Sms/summary-response.json"), Smtp2GoJsonContext.Default.ApiResponseSmsSummary)!;
        ApiResponse<SmsReceivedResult> received = JsonSerializer.Deserialize(Fixture.Read("Sms/view-received-response.json"), Smtp2GoJsonContext.Default.ApiResponseSmsReceivedResult)!;
        ApiResponse<SmsSentResult> viewedSent = JsonSerializer.Deserialize(Fixture.Read("Sms/view-sent-response.json"), Smtp2GoJsonContext.Default.ApiResponseSmsSentResult)!;

        sent.Data.Statuses.Should().Equal(new Dictionary<string, int> { ["queued"] = 1 });
        sent.Data.TotalSent.Should().Be(1);
        sent.Data.Messages.Should().BeNull();
        sent.Data.Extra.Should().BeNull();

        summary.Data.TotalMessages.Should().Be(123);
        summary.Data.TotalUnits.Should().Be(156);
        summary.Data.TotalCost.Should().Be(10.456m);
        summary.Data.Subaccounts.Should().HaveCount(2);
        summary.Data.Subaccounts![1].Should().BeEquivalentTo(new SmsSubaccountSummary { SubaccountId = "EqS3x", TotalMessages = 73, TotalUnits = 96, TotalCost = 5.206m });
        summary.Data.Subaccounts.Should().OnlyContain(s => s.Extra == null);
        summary.Data.Extra.Should().BeNull();

        ReceivedSms message = received.Data.Messages.Should().ContainSingle().Which;
        message.SourceAddress.Should().Be("15185550120", because: "the docs example carries the number as a JSON number");
        message.DestinationAddress.Should().Be("15185550141");
        message.Timestamp.Should().Be(new DateTimeOffset(2022, 9, 30, 2, 2, 41, TimeSpan.Zero));
        message.Content.Should().Be("Example content");
        message.MessageId.Should().Be("4c1d0952-1c91-48ab-9a72-5221281c0c95");
        message.Username.Should().Be("api-12345678");
        message.Extra.Should().BeNull();
        received.Data.Extra.Should().BeNull();

        SentSms sentMessage = viewedSent.Data.Messages.Should().ContainSingle().Which;
        sentMessage.Id.Should().Be("11170632-25c9-4fbd-85b3-7491fa506d74");
        sentMessage.Timestamp.Should().Be(new DateTimeOffset(2025, 6, 8, 19, 5, 25, 830, TimeSpan.Zero));
        sentMessage.Sender.Should().Be("shared");
        sentMessage.SenderEmail.Should().Be("test@example.com");
        sentMessage.DestinationAddress.Should().Be("+123456789");
        sentMessage.DestinationAddressCountry.Should().Be("US");
        sentMessage.Format.Should().Be("SMS");
        sentMessage.Status.Should().Be("Message discarded");
        sentMessage.Units.Should().Be(1);
        sentMessage.Extra.Should().BeNull();
        viewedSent.Data.Extra.Should().BeNull();
    }

    [Fact]
    public void Send_messages_from_the_schema_deserialise_with_tolerant_statuses()
    {
        ApiResponse<SmsSendResult> sent = JsonSerializer.Deserialize("""{"request_id":"r","data":{"statuses":{"queued":1,"failed":1},"total_sent":2,"messages":[{"destination":"+12025550959","message_id":"m1","status":"queued"},{"destination":"+1","message_id":"m2","status":"blackholed"}]}}""", Smtp2GoJsonContext.Default.ApiResponseSmsSendResult)!;

        sent.Data.Messages.Should().HaveCount(2);
        sent.Data.Messages![0].Status.Should().Be(SmsStatus.Queued);
        sent.Data.Messages[1].Status.Should().Be(SmsStatus.Unknown);
        sent.Data.Statuses!["failed"].Should().Be(1);
    }

    [Fact]
    public void Requests_are_validated()
    {
        List<string> errors = [];
        ((IRequestValidator)new SmsSendRequest { Destination = [], Content = "" }).Validate(EndpointTable.Get("sms/send"), errors);
        errors.Should().Equal("destination must list at least one number.", "content is required.");

        errors.Clear();
        ((IRequestValidator)new SmsSendRequest { Destination = [.. Enumerable.Repeat("+1", 101)], Content = "hi" }).Validate(EndpointTable.Get("sms/send"), errors);
        errors.Should().Equal("destination must list at most 100 numbers.");

        errors.Clear();
        ((IRequestValidator)new SmsSendRequest { Destination = ["+1", " "], Content = "hi" }).Validate(EndpointTable.Get("sms/send"), errors);
        errors.Should().Equal("destination[1] must not be blank.");

        errors.Clear();
        DateTimeOffset start = new(2026, 9, 8, 0, 0, 0, TimeSpan.Zero);
        ((IRequestValidator)new SmsSummaryRequest { StartDate = start, EndDate = start.AddDays(-1) }).Validate(EndpointTable.Get("sms/summary"), errors);
        ((IRequestValidator)new SmsReceivedRequest { StartDate = start, EndDate = start.AddDays(-1) }).Validate(EndpointTable.Get("sms/view-received"), errors);
        ((IRequestValidator)new SmsSentRequest { StartDate = start, EndDate = start }).Validate(EndpointTable.Get("sms/view-sent"), errors);
        errors.Should().Equal("start_date must not be after end_date.", "start_date must not be after end_date.");
    }

    [Fact]
    public async Task Client_methods_post_to_the_documented_paths_and_inject_the_subaccount_id_where_documented()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler()
            .Respond("sms/send", HttpStatusCode.OK, Fixture.Read("Sms/send-response.json"))
            .Respond("sms/summary", HttpStatusCode.OK, Fixture.Read("Sms/summary-response.json"))
            .Respond("sms/view-received", HttpStatusCode.OK, Fixture.Read("Sms/view-received-response.json"))
            .Respond("sms/view-sent", HttpStatusCode.OK, Fixture.Read("Sms/view-sent-response.json"));
        Smtp2GoClient client = TestClient.Create(handler, o => o.DefaultSubaccountId = "sub-1");

        (await client.Sms.SendAsync(new SmsSendRequest { Destination = ["12025550959"], Content = "hi" })).Data.TotalSent.Should().Be(1);
        (await client.Sms.GetSummaryAsync(new SmsSummaryRequest())).Data.TotalMessages.Should().Be(123);
        (await client.Sms.ViewReceivedAsync(new SmsReceivedRequest { Username = "api-12345678" })).Data.Messages.Should().HaveCount(1);
        (await client.Sms.ViewSentAsync(new SmsSentRequest())).Data.Messages.Should().HaveCount(1);

        handler.Requests.Select(r => r.Endpoint!.Path).Should().Equal("sms/send", "sms/summary", "sms/view-received", "sms/view-sent");
        handler.Requests[0].Body.Should().Be("""{"destination":["12025550959"],"content":"hi"}""", because: "sms/send does not document subaccount_id");
        handler.Requests[1].Body.Should().Be("""{"subaccount_id":"sub-1"}""");
        handler.Requests[2].Body.Should().Be("""{"username":"api-12345678","subaccount_id":"sub-1"}""");
        handler.Requests[3].Body.Should().Be("""{"subaccount_id":"sub-1"}""");
    }

    [Fact]
    public async Task Null_requests_are_rejected()
    {
        Smtp2GoClient client = TestClient.Create(new FakeHttpMessageHandler());

        await ((Func<Task>)(() => client.Sms.SendAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.Sms.GetSummaryAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.Sms.ViewReceivedAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.Sms.ViewSentAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
    }
}
