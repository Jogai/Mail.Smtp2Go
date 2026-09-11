using System.Net;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Tests.Unit.Email;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.SingleSenders;

public class SingleSenderTests
{
    [Fact]
    public void Single_sender_family_is_seeded_and_accepts_subaccount_id()
    {
        IEnumerable<Endpoint> family = EndpointTable.All.Where(e => e.Path.StartsWith("single_sender_emails/", StringComparison.Ordinal));

        family.Select(e => e.Path).Should().BeEquivalentTo("single_sender_emails/view", "single_sender_emails/add", "single_sender_emails/remove");
        family.Should().OnlyContain(e => e.Method == HttpMethod.Post && e.AcceptsSubaccountId && e.RateLimit == RateLimitClass.None);
        family.Where(e => e.Idempotent).Select(e => e.Path).Should().Equal("single_sender_emails/view");
    }

    [Fact]
    public void Requests_serialise_the_documented_fields()
    {
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(new SingleSenderAddRequest { EmailAddress = "send@example.com", Message = "Please verify this address for the newsletter." }, Smtp2GoJsonContext.Default.SingleSenderAddRequest), "SingleSenders/add-request.json");
        JsonSerializer.Serialize(new SingleSenderAddRequest { EmailAddress = "send@example.com" }, Smtp2GoJsonContext.Default.SingleSenderAddRequest).Should().Be("""{"email_address":"send@example.com"}""");
        JsonSerializer.Serialize(new SingleSenderViewRequest(), Smtp2GoJsonContext.Default.SingleSenderViewRequest).Should().Be("{}");
        JsonSerializer.Serialize(new SingleSenderViewRequest { EmailAddress = "test@test.com" }, Smtp2GoJsonContext.Default.SingleSenderViewRequest).Should().Be("""{"email_address":"test@test.com"}""");
    }

    [Fact]
    public void Fixture_responses_deserialise_with_an_empty_extra()
    {
        ApiResponse<SingleSenderViewResult> viewed = JsonSerializer.Deserialize(Fixture.Read("SingleSenders/view-response.json"), Smtp2GoJsonContext.Default.ApiResponseSingleSenderViewResult)!;
        ApiResponse<JsonElement> added = JsonSerializer.Deserialize(Fixture.Read("SingleSenders/add-response.json"), Smtp2GoJsonContext.Default.ApiResponseJsonElement)!;
        ApiResponse<string> removed = JsonSerializer.Deserialize(Fixture.Read("SingleSenders/remove-response.json"), Smtp2GoJsonContext.Default.ApiResponseString)!;

        SingleSenderEmail sender = viewed.Data.Senders.Should().ContainSingle().Which;
        sender.EmailAddress.Should().Be("test@test.com");
        sender.Verified.Should().BeTrue();
        sender.Extra.Should().BeNull();
        viewed.Data.Extra.Should().BeNull();
        added.Data.GetProperty("just_returns_this").GetString().Should().Be("ok");
        removed.Data.Should().Be("OK");
    }

    [Fact]
    public void Docs_view_example_with_request_id_inside_data_is_still_readable()
    {
        // The docs example (nested request_id, none at the top level) does not match the envelope; if the live API ever answered like that, the nested field would land in Extra.
        ApiResponse<SingleSenderViewResult> viewed = JsonSerializer.Deserialize("""{"request_id":"top","data":{"request_id":"nested","senders":[]}}""", Smtp2GoJsonContext.Default.ApiResponseSingleSenderViewResult)!;

        viewed.RequestId.Should().Be("top");
        viewed.Data.Extra.Should().ContainKey("request_id");
    }

    [Fact]
    public void Requests_are_validated()
    {
        List<string> errors = [];
        ((IRequestValidator)new SingleSenderAddRequest { EmailAddress = " " }).Validate(EndpointTable.Get("single_sender_emails/add"), errors);
        errors.Should().Equal("email_address is required.");

        errors.Clear();
        ((IRequestValidator)new SingleSenderAddRequest { EmailAddress = "nope" }).Validate(EndpointTable.Get("single_sender_emails/add"), errors);
        errors.Should().Equal("email_address must be an email address.");
    }

    [Fact]
    public async Task Client_methods_post_to_the_documented_paths_and_inject_the_subaccount_id()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler()
            .Respond("single_sender_emails/view", HttpStatusCode.OK, Fixture.Read("SingleSenders/view-response.json"))
            .Respond("single_sender_emails/add", HttpStatusCode.OK, Fixture.Read("SingleSenders/add-response.json"))
            .Respond("single_sender_emails/remove", HttpStatusCode.OK, Fixture.Read("SingleSenders/remove-response.json"));
        Smtp2GoClient client = TestClient.Create(handler, o => o.DefaultSubaccountId = "sub-1");

        (await client.SingleSenders.ViewAsync(new SingleSenderViewRequest { EmailAddress = "test@test.com" })).Data.Senders.Should().HaveCount(1);
        (await client.SingleSenders.AddAsync(new SingleSenderAddRequest { EmailAddress = "send@example.com" })).Data.ValueKind.Should().Be(JsonValueKind.Object);
        (await client.SingleSenders.RemoveAsync("send@example.com")).Data.Should().Be("OK");

        handler.Requests.Select(r => r.Endpoint!.Path).Should().Equal("single_sender_emails/view", "single_sender_emails/add", "single_sender_emails/remove");
        handler.Requests[0].Body.Should().Be("""{"email_address":"test@test.com","subaccount_id":"sub-1"}""");
        handler.Requests[1].Body.Should().Be("""{"email_address":"send@example.com","subaccount_id":"sub-1"}""");
        handler.Requests[2].Body.Should().Be("""{"email_address":"send@example.com","subaccount_id":"sub-1"}""");
    }

    [Fact]
    public async Task Null_and_blank_arguments_are_rejected()
    {
        Smtp2GoClient client = TestClient.Create(new FakeHttpMessageHandler());

        await ((Func<Task>)(() => client.SingleSenders.ViewAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.SingleSenders.AddAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.SingleSenders.RemoveAsync(""))).Should().ThrowAsync<ArgumentException>();
    }
}
