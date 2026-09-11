using System.Net;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Tests.Unit.Email;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.AllowedRecipients;

public class AllowedRecipientTests
{
    [Fact]
    public void Allowed_recipients_family_is_seeded_and_accepts_subaccount_id()
    {
        IEnumerable<Endpoint> family = EndpointTable.All.Where(e => e.Path.StartsWith("allowed_recipients/", StringComparison.Ordinal));

        family.Select(e => e.Path).Should().BeEquivalentTo("allowed_recipients/view", "allowed_recipients/add", "allowed_recipients/remove", "allowed_recipients/update");
        family.Should().OnlyContain(e => e.Method == HttpMethod.Post && e.AcceptsSubaccountId && e.RateLimit == RateLimitClass.None);
        family.Where(e => e.Idempotent).Select(e => e.Path).Should().Equal("allowed_recipients/view");
    }

    [Fact]
    public void Requests_serialise_the_documented_fields()
    {
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(new AllowedRecipientsUpdateRequest { AllowedRecipients = ["test-person@example.com", "otherexample.com"], Enabled = true }, Smtp2GoJsonContext.Default.AllowedRecipientsUpdateRequest), "AllowedRecipients/update-request.json");
        JsonSerializer.Serialize(new AllowedRecipientsAddRequest { AllowedRecipients = ["test@test.com"], Enabled = true }, Smtp2GoJsonContext.Default.AllowedRecipientsAddRequest).Should().Be("""{"allowed_recipients":["test@test.com"],"enabled":true}""");
        JsonSerializer.Serialize(new AllowedRecipientsRemoveRequest { AllowedRecipients = ["test@test.com"] }, Smtp2GoJsonContext.Default.AllowedRecipientsRemoveRequest).Should().Be("""{"allowed_recipients":["test@test.com"]}""");
        JsonSerializer.Serialize(new AllowedRecipientsUpdateRequest { AllowedRecipients = [], Enabled = false }, Smtp2GoJsonContext.Default.AllowedRecipientsUpdateRequest).Should().Be("""{"allowed_recipients":[],"enabled":false}""");
    }

    [Fact]
    public void Docs_responses_deserialise_with_an_empty_extra()
    {
        ApiResponse<AllowedRecipientsList> viewed = JsonSerializer.Deserialize(Fixture.Read("AllowedRecipients/view-response.json"), Smtp2GoJsonContext.Default.ApiResponseAllowedRecipientsList)!;
        ApiResponse<AllowedRecipientsList> updated = JsonSerializer.Deserialize(Fixture.Read("AllowedRecipients/update-response.json"), Smtp2GoJsonContext.Default.ApiResponseAllowedRecipientsList)!;

        viewed.Data.AllowedRecipients.Should().Equal("test@test.com");
        viewed.Data.Enabled.Should().BeTrue();
        viewed.Data.Extra.Should().BeNull();
        updated.Data.AllowedRecipients.Should().Equal("test-person@example.com", "otherexample.com");
        updated.Data.Extra.Should().BeNull();
    }

    [Fact]
    public void Requests_are_validated()
    {
        List<string> errors = [];
        ((IRequestValidator)new AllowedRecipientsAddRequest { AllowedRecipients = [] }).Validate(EndpointTable.Get("allowed_recipients/add"), errors);
        errors.Should().Equal("allowed_recipients must list at least one address or domain.");

        errors.Clear();
        ((IRequestValidator)new AllowedRecipientsRemoveRequest { AllowedRecipients = [""] }).Validate(EndpointTable.Get("allowed_recipients/remove"), errors);
        errors.Should().Equal("allowed_recipients[0] must not be blank.");

        errors.Clear();
        ((IRequestValidator)new AllowedRecipientsUpdateRequest { AllowedRecipients = [], Enabled = false }).Validate(EndpointTable.Get("allowed_recipients/update"), errors);
        errors.Should().BeEmpty(because: "update may clear the list");
    }

    [Fact]
    public async Task Client_methods_post_to_the_documented_paths_and_inject_the_subaccount_id()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler()
            .Respond("allowed_recipients/view", HttpStatusCode.OK, Fixture.Read("AllowedRecipients/view-response.json"))
            .Respond("allowed_recipients/add", HttpStatusCode.OK, Fixture.Read("AllowedRecipients/view-response.json"))
            .Respond("allowed_recipients/remove", HttpStatusCode.OK, Fixture.Read("AllowedRecipients/view-response.json"))
            .Respond("allowed_recipients/update", HttpStatusCode.OK, Fixture.Read("AllowedRecipients/update-response.json"));
        Smtp2GoClient client = TestClient.Create(handler, o => o.DefaultSubaccountId = "sub-1");

        (await client.AllowedRecipients.ViewAsync()).Data.Enabled.Should().BeTrue();
        (await client.AllowedRecipients.AddAsync(new AllowedRecipientsAddRequest { AllowedRecipients = ["test@test.com"] })).Data.AllowedRecipients.Should().HaveCount(1);
        (await client.AllowedRecipients.RemoveAsync(new AllowedRecipientsRemoveRequest { AllowedRecipients = ["gone@test.com"], Enabled = false })).Data.AllowedRecipients.Should().HaveCount(1);
        (await client.AllowedRecipients.UpdateAsync(new AllowedRecipientsUpdateRequest { AllowedRecipients = ["test-person@example.com"], Enabled = true })).Data.AllowedRecipients.Should().HaveCount(2);

        handler.Requests.Select(r => r.Endpoint!.Path).Should().Equal("allowed_recipients/view", "allowed_recipients/add", "allowed_recipients/remove", "allowed_recipients/update");
        handler.Requests[0].Body.Should().Be("""{"subaccount_id":"sub-1"}""");
        handler.Requests[1].Body.Should().Be("""{"allowed_recipients":["test@test.com"],"subaccount_id":"sub-1"}""");
        handler.Requests[2].Body.Should().Be("""{"allowed_recipients":["gone@test.com"],"enabled":false,"subaccount_id":"sub-1"}""");
        handler.Requests[3].Body.Should().Be("""{"allowed_recipients":["test-person@example.com"],"enabled":true,"subaccount_id":"sub-1"}""");
    }

    [Fact]
    public async Task Null_requests_are_rejected()
    {
        Smtp2GoClient client = TestClient.Create(new FakeHttpMessageHandler());

        await ((Func<Task>)(() => client.AllowedRecipients.AddAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.AllowedRecipients.RemoveAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.AllowedRecipients.UpdateAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
    }
}
