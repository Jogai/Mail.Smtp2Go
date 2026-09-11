using System.Net;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Tests.Unit.Email;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.AllowedSenders;

public class AllowedSenderTests
{
    [Fact]
    public void Allowed_senders_family_is_seeded_and_accepts_subaccount_id()
    {
        IEnumerable<Endpoint> family = EndpointTable.All.Where(e => e.Path.StartsWith("allowed_senders/", StringComparison.Ordinal));

        family.Select(e => e.Path).Should().BeEquivalentTo("allowed_senders/view", "allowed_senders/add", "allowed_senders/remove", "allowed_senders/update");
        family.Should().OnlyContain(e => e.Method == HttpMethod.Post && e.AcceptsSubaccountId && e.RateLimit == RateLimitClass.None);
        family.Where(e => e.Idempotent).Select(e => e.Path).Should().Equal("allowed_senders/view");
    }

    [Fact]
    public void Requests_serialise_the_documented_fields()
    {
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(new AllowedSendersUpdateRequest { AllowedSenders = ["test-person@example.com", "otherexample.com"], Mode = AllowedSendersMode.Whitelist }, Smtp2GoJsonContext.Default.AllowedSendersUpdateRequest), "AllowedSenders/update-request.json");
        JsonSerializer.Serialize(new AllowedSendersAddRequest { AllowedSenders = ["test@test.com"] }, Smtp2GoJsonContext.Default.AllowedSendersAddRequest).Should().Be("""{"allowed_senders":["test@test.com"]}""");
        JsonSerializer.Serialize(new AllowedSendersRemoveRequest { AllowedSenders = ["test@test.com"] }, Smtp2GoJsonContext.Default.AllowedSendersRemoveRequest).Should().Be("""{"allowed_senders":["test@test.com"]}""");
        JsonSerializer.Serialize(new AllowedSendersUpdateRequest { AllowedSenders = [], Mode = AllowedSendersMode.Disabled }, Smtp2GoJsonContext.Default.AllowedSendersUpdateRequest).Should().Be("""{"allowed_senders":[],"mode":"disabled"}""");
    }

    [Fact]
    public void Docs_responses_deserialise_with_an_empty_extra()
    {
        ApiResponse<AllowedSendersList> viewed = JsonSerializer.Deserialize(Fixture.Read("AllowedSenders/view-response.json"), Smtp2GoJsonContext.Default.ApiResponseAllowedSendersList)!;
        ApiResponse<AllowedSendersList> updated = JsonSerializer.Deserialize(Fixture.Read("AllowedSenders/update-response.json"), Smtp2GoJsonContext.Default.ApiResponseAllowedSendersList)!;

        viewed.Data.AllowedSenders.Should().Equal("test@test.com");
        viewed.Data.Mode.Should().Be(AllowedSendersMode.Disabled);
        viewed.Data.Extra.Should().BeNull();
        updated.Data.AllowedSenders.Should().Equal("test-person@example.com", "otherexample.com");
        updated.Data.Mode.Should().Be(AllowedSendersMode.Whitelist);
        updated.Data.Extra.Should().BeNull();

        JsonSerializer.Deserialize("""{"request_id":"r","data":{"allowed_senders":[],"mode":"greylist"}}""", Smtp2GoJsonContext.Default.ApiResponseAllowedSendersList)!.Data.Mode.Should().Be(AllowedSendersMode.Unknown);
    }

    [Fact]
    public void Requests_are_validated()
    {
        List<string> errors = [];
        ((IRequestValidator)new AllowedSendersAddRequest { AllowedSenders = [] }).Validate(EndpointTable.Get("allowed_senders/add"), errors);
        errors.Should().Equal("allowed_senders must list at least one address or domain.");

        errors.Clear();
        ((IRequestValidator)new AllowedSendersRemoveRequest { AllowedSenders = ["a@b.c", " "] }).Validate(EndpointTable.Get("allowed_senders/remove"), errors);
        errors.Should().Equal("allowed_senders[1] must not be blank.");

        errors.Clear();
        ((IRequestValidator)new AllowedSendersUpdateRequest { AllowedSenders = null!, Mode = AllowedSendersMode.Unknown }).Validate(EndpointTable.Get("allowed_senders/update"), errors);
        errors.Should().Equal("allowed_senders is required.", "mode must not be AllowedSendersMode.Unknown.");

        errors.Clear();
        ((IRequestValidator)new AllowedSendersUpdateRequest { AllowedSenders = [], Mode = AllowedSendersMode.Disabled }).Validate(EndpointTable.Get("allowed_senders/update"), errors);
        errors.Should().BeEmpty(because: "update may clear the list");
    }

    [Fact]
    public async Task Client_methods_post_to_the_documented_paths_and_inject_the_subaccount_id()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler()
            .Respond("allowed_senders/view", HttpStatusCode.OK, Fixture.Read("AllowedSenders/view-response.json"))
            .Respond("allowed_senders/add", HttpStatusCode.OK, Fixture.Read("AllowedSenders/view-response.json"))
            .Respond("allowed_senders/remove", HttpStatusCode.OK, Fixture.Read("AllowedSenders/view-response.json"))
            .Respond("allowed_senders/update", HttpStatusCode.OK, Fixture.Read("AllowedSenders/update-response.json"));
        Smtp2GoClient client = TestClient.Create(handler, o => o.DefaultSubaccountId = "sub-1");

        (await client.AllowedSenders.ViewAsync()).Data.Mode.Should().Be(AllowedSendersMode.Disabled);
        (await client.AllowedSenders.AddAsync(new AllowedSendersAddRequest { AllowedSenders = ["test@test.com"] })).Data.AllowedSenders.Should().HaveCount(1);
        (await client.AllowedSenders.RemoveAsync(new AllowedSendersRemoveRequest { AllowedSenders = ["gone@test.com"] })).Data.AllowedSenders.Should().HaveCount(1);
        (await client.AllowedSenders.UpdateAsync(new AllowedSendersUpdateRequest { AllowedSenders = ["test-person@example.com"], Mode = AllowedSendersMode.Whitelist })).Data.Mode.Should().Be(AllowedSendersMode.Whitelist);

        handler.Requests.Select(r => r.Endpoint!.Path).Should().Equal("allowed_senders/view", "allowed_senders/add", "allowed_senders/remove", "allowed_senders/update");
        handler.Requests[0].Body.Should().Be("""{"subaccount_id":"sub-1"}""");
        handler.Requests[1].Body.Should().Be("""{"allowed_senders":["test@test.com"],"subaccount_id":"sub-1"}""");
        handler.Requests[2].Body.Should().Be("""{"allowed_senders":["gone@test.com"],"subaccount_id":"sub-1"}""");
        handler.Requests[3].Body.Should().Be("""{"allowed_senders":["test-person@example.com"],"mode":"whitelist","subaccount_id":"sub-1"}""");
    }

    [Fact]
    public async Task Null_requests_are_rejected()
    {
        Smtp2GoClient client = TestClient.Create(new FakeHttpMessageHandler());

        await ((Func<Task>)(() => client.AllowedSenders.AddAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.AllowedSenders.RemoveAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.AllowedSenders.UpdateAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
    }
}
