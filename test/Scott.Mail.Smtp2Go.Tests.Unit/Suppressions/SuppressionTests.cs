using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Tests.Unit.Email;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Suppressions;

public class SuppressionTests
{
    [Fact]
    public void Suppression_family_is_seeded_and_accepts_subaccount_id()
    {
        IEnumerable<Endpoint> family = EndpointTable.All.Where(e => e.Path.StartsWith("suppression/", StringComparison.Ordinal));

        family.Select(e => e.Path).Should().BeEquivalentTo("suppression/add", "suppression/view", "suppression/remove");
        family.Should().OnlyContain(e => e.AcceptsSubaccountId && e.RateLimit == RateLimitClass.None);
        family.Where(e => e.Idempotent).Select(e => e.Path).Should().Equal("suppression/view");
    }

    [Fact]
    public void Add_request_serialises_the_documented_fields()
    {
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(new SuppressionAddRequest { EmailAddress = "temp@example.com", BlockDescription = "no longer a customer" }, Smtp2GoJsonContext.Default.SuppressionAddRequest), "Suppressions/add-request.json");
    }

    [Fact]
    public void View_request_serialises_every_filter()
    {
        SuppressionViewRequest request = new()
        {
            ContinueToken = "eyJwYWdlIjoyfQ==",
            EmailAddress = "temp@example.com",
            StartDate = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
            Fuzzy = true,
            Reason = "manual",
            Reasons = ["manual", "spam"],
            Recipient = "temp",
            Recipients = ["temp@example.com", "example.org"],
            Sort = SortDirection.Desc,
            SuppressionType = SuppressionType.Manual,
            SuppressionTypes = [SuppressionType.Manual, SuppressionType.Spam, SuppressionType.Unsubscribe, SuppressionType.Bounce, SuppressionType.Compliance],
            Wildcard = "example",
        };

        Golden.AssertMatchesFixture(JsonSerializer.Serialize(request, Smtp2GoJsonContext.Default.SuppressionViewRequest), "Suppressions/view-request-full.json");
        JsonSerializer.Serialize(new SuppressionViewRequest(), Smtp2GoJsonContext.Default.SuppressionViewRequest).Should().Be("{}");
    }

    [Fact]
    public void Remove_request_serialises_the_reasons_as_lower_case_names()
    {
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(new SuppressionRemoveRequest { EmailAddress = "temp@test.com", Reasons = [SuppressionType.Manual, SuppressionType.Spam] }, Smtp2GoJsonContext.Default.SuppressionRemoveRequest), "Suppressions/remove-request.json");
    }

    [Fact]
    public void Docs_responses_deserialise_with_an_empty_extra()
    {
        ApiResponse<SuppressionAddResult> added = JsonSerializer.Deserialize(Fixture.Read("Suppressions/add-response.json"), Smtp2GoJsonContext.Default.ApiResponseSuppressionAddResult)!;
        ApiResponse<SuppressionViewResult> viewed = JsonSerializer.Deserialize(Fixture.Read("Suppressions/view-response.json"), Smtp2GoJsonContext.Default.ApiResponseSuppressionViewResult)!;
        ApiResponse<SuppressionRemoveResult> removed = JsonSerializer.Deserialize(Fixture.Read("Suppressions/remove-response.json"), Smtp2GoJsonContext.Default.ApiResponseSuppressionRemoveResult)!;

        added.Data.Should().BeEquivalentTo(new SuppressionAddResult { Added = true, BlockDescription = "", EmailAddress = "temp@example.com" });
        added.Data.Extra.Should().BeNull();

        viewed.Data.ContinueToken.Should().BeNull();
        viewed.Data.TotalResults.Should().Be(1);
        Suppression suppression = viewed.Data.Results.Should().ContainSingle().Which;
        suppression.EmailAddress.Should().Be("temp@example.com");
        suppression.ReasonRaw.Should().Be("manual");
        suppression.Reason.Should().Be(SuppressionType.Manual);
        suppression.Complaint.Should().BeEmpty();
        suppression.BlockDescription.Should().BeEmpty();
        suppression.Subject.Should().BeNull();
        suppression.Timestamp.Should().Be(new DateTimeOffset(2022, 11, 14, 7, 54, 45, TimeSpan.Zero));
        suppression.Extra.Should().BeNull();
        viewed.Data.Extra.Should().BeNull();

        removed.Data.Suppressions.Should().HaveCount(2);
        removed.Data.Suppressions![0].Should().BeEquivalentTo(new SuppressionRemoval { EmailAddress = "temp@test.com", ReasonRaw = "manual", Removed = true });
        removed.Data.Suppressions[0].Reason.Should().Be(SuppressionType.Manual);
        removed.Data.Suppressions[1].Reason.Should().Be(SuppressionType.Spam);
        removed.Data.Suppressions[1].Removed.Should().BeFalse();
        removed.Data.Suppressions.Should().OnlyContain(s => s.Extra == null);
        removed.Data.Extra.Should().BeNull();
    }

    [Fact]
    public void Unknown_reasons_are_readable_raw()
    {
        ApiResponse<SuppressionViewResult> viewed = JsonSerializer.Deserialize("""{"request_id":"r","data":{"results":[{"email_address":"a@b.c","reason":"legal-hold"}]}}""", Smtp2GoJsonContext.Default.ApiResponseSuppressionViewResult)!;

        viewed.Data.Results![0].Reason.Should().Be(SuppressionType.Unknown);
        viewed.Data.Results[0].ReasonRaw.Should().Be("legal-hold");
    }

    [Fact]
    public void Requests_are_validated()
    {
        List<string> errors = [];
        ((IRequestValidator)new SuppressionAddRequest { EmailAddress = " " }).Validate(EndpointTable.Get("suppression/add"), errors);
        errors.Should().Equal("email_address is required.");

        errors.Clear();
        ((IRequestValidator)new SuppressionRemoveRequest { EmailAddress = "a@b.c", Reasons = [] }).Validate(EndpointTable.Get("suppression/remove"), errors);
        errors.Should().Equal("reasons must list at least one suppression type.");

        errors.Clear();
        ((IRequestValidator)new SuppressionRemoveRequest { EmailAddress = "", Reasons = [SuppressionType.Unknown] }).Validate(EndpointTable.Get("suppression/remove"), errors);
        errors.Should().Equal("email_address is required.", "reasons must not contain SuppressionType.Unknown.");

        errors.Clear();
        ((IRequestValidator)new SuppressionViewRequest { Sort = SortDirection.Unknown, SuppressionTypes = [SuppressionType.Unknown] }).Validate(EndpointTable.Get("suppression/view"), errors);
        errors.Should().Equal("sort must not be SortDirection.Unknown.", "suppression_type(s) must not contain SuppressionType.Unknown.");
    }

    [Fact]
    public async Task Client_methods_post_to_the_documented_paths_and_inject_the_subaccount_id()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler()
            .Respond("suppression/add", HttpStatusCode.OK, Fixture.Read("Suppressions/add-response.json"))
            .Respond("suppression/view", HttpStatusCode.OK, Fixture.Read("Suppressions/view-response.json"))
            .Respond("suppression/remove", HttpStatusCode.OK, Fixture.Read("Suppressions/remove-response.json"));
        Smtp2GoClient client = TestClient.Create(handler, o => o.DefaultSubaccountId = "sub-1");

        (await client.Suppressions.AddAsync(new SuppressionAddRequest { EmailAddress = "temp@example.com" })).Data.Added.Should().BeTrue();
        (await client.Suppressions.ViewAsync(new SuppressionViewRequest { EmailAddress = "temp@example.com" })).Data.TotalResults.Should().Be(1);
        (await client.Suppressions.RemoveAsync(new SuppressionRemoveRequest { EmailAddress = "temp@test.com", Reasons = [SuppressionType.Manual] })).Data.Suppressions.Should().HaveCount(2);

        handler.Requests.Select(r => r.Endpoint!.Path).Should().Equal("suppression/add", "suppression/view", "suppression/remove");
        handler.Requests[0].Body.Should().Be("""{"email_address":"temp@example.com","subaccount_id":"sub-1"}""");
        handler.Requests[1].Body.Should().Be("""{"email_address":"temp@example.com","subaccount_id":"sub-1"}""");
        handler.Requests[2].Body.Should().Be("""{"email_address":"temp@test.com","reasons":["manual"],"subaccount_id":"sub-1"}""");
    }

    [Fact]
    public async Task ViewAllAsync_follows_continue_tokens()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("suppression/view", async (request, ct) =>
        {
            string? token = JsonNode.Parse(await request.Content!.ReadAsStringAsync(ct))!["continue_token"]?.GetValue<string>();
            string page = token is null ? """{"results":[{"email_address":"a@x.y"},{"email_address":"b@x.y"}],"continue_token":"next","total_results":3}""" : """{"results":[{"email_address":"c@x.y"}],"continue_token":null,"total_results":3}""";
            return FakeHttpMessageHandler.Json(HttpStatusCode.OK, $$"""{"request_id":"r","data":{{page}}}""");
        });
        Smtp2GoClient client = TestClient.Create(handler);

        List<string?> addresses = [];
        await foreach (Suppression suppression in client.Suppressions.ViewAllAsync(new SuppressionViewRequest { Wildcard = "x.y" }, cancellationToken: TestContext.Current.CancellationToken))
        {
            addresses.Add(suppression.EmailAddress);
        }

        addresses.Should().Equal("a@x.y", "b@x.y", "c@x.y");
        handler.Requests.Should().HaveCount(2);
        handler.Requests.Should().OnlyContain(r => r.Body!.Contains("\"wildcard\":\"x.y\""));
    }

    [Fact]
    public async Task Null_requests_are_rejected()
    {
        Smtp2GoClient client = TestClient.Create(new FakeHttpMessageHandler());

        await ((Func<Task>)(() => client.Suppressions.AddAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.Suppressions.ViewAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.Suppressions.RemoveAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        ((Action)(() => client.Suppressions.ViewAllAsync(null!))).Should().Throw<ArgumentNullException>();
    }
}
