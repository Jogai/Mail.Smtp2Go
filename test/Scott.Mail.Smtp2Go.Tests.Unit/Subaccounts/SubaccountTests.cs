using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Tests.Unit.Email;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Subaccounts;

public class SubaccountTests
{
    [Fact]
    public void Subaccount_family_is_seeded_without_subaccount_id()
    {
        IEnumerable<Endpoint> family = EndpointTable.All.Where(e => e.Path.StartsWith("subaccount", StringComparison.Ordinal));

        family.Select(e => e.Path).Should().BeEquivalentTo("subaccounts/search", "subaccount/add", "subaccount/edit", "subaccount/close", "subaccount/reopen");
        family.Should().OnlyContain(e => e.Method == HttpMethod.Post && !e.AcceptsSubaccountId);
        family.Where(e => e.Idempotent).Select(e => e.Path).Should().Equal("subaccounts/search");
        EndpointTable.Get("subaccount/add").RateLimit.Should().Be(RateLimitClass.SubaccountAdd);
        family.Where(e => e.Path != "subaccount/add").Should().OnlyContain(e => e.RateLimit == RateLimitClass.None);
    }

    [Fact]
    public void Requests_serialise_the_documented_fields()
    {
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(new SubaccountAddRequest { FullName = "Test Person", SubaccountEmail = "test@example.com", Limit = 10000, DedicatedIp = false, Archiving = true, Enforce2fa = true, EnableSms = true, SmsLimit = 1000 }, Smtp2GoJsonContext.Default.SubaccountAddRequest), "Subaccounts/add-request.json");
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(new SubaccountEditRequest { Id = "34l8oj", FullName = "Renamed Person", Limit = 20000, DedicatedIp = false, Archiving = false, Enforce2fa = false, EnableSms = false, SmsLimit = 0 }, Smtp2GoJsonContext.Default.SubaccountEditRequest), "Subaccounts/edit-request.json");
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(new SubaccountSearchRequest { FuzzySearch = false, SearchTerms = ["Test", "example.com"], States = SubaccountStateFilter.Active, SortDirection = SortDirection.Desc, PageSize = 50, ContinueToken = "eyJwYWdlIjoyfQ==" }, Smtp2GoJsonContext.Default.SubaccountSearchRequest), "Subaccounts/search-request.json");
        JsonSerializer.Serialize(new SubaccountSearchRequest(), Smtp2GoJsonContext.Default.SubaccountSearchRequest).Should().Be("{}");
        JsonSerializer.Serialize(new SubaccountSearchRequest { States = SubaccountStateFilter.All }, Smtp2GoJsonContext.Default.SubaccountSearchRequest).Should().Be("""{"states":"all"}""");
        JsonSerializer.Serialize(new SubaccountCloseRequest { Id = "34l8oj", Email = "test@example.com" }, Smtp2GoJsonContext.Default.SubaccountCloseRequest).Should().Be("""{"id":"34l8oj","email":"test@example.com"}""");
        JsonSerializer.Serialize(new SubaccountReopenRequest { Id = "34l8oj" }, Smtp2GoJsonContext.Default.SubaccountReopenRequest).Should().Be("""{"id":"34l8oj"}""");
    }

    [Fact]
    public void Docs_responses_deserialise_with_an_empty_extra()
    {
        ApiResponse<Subaccount> added = JsonSerializer.Deserialize(Fixture.Read("Subaccounts/add-response.json"), Smtp2GoJsonContext.Default.ApiResponseSubaccount)!;
        ApiResponse<SubaccountSearchResult> searched = JsonSerializer.Deserialize(Fixture.Read("Subaccounts/search-response.json"), Smtp2GoJsonContext.Default.ApiResponseSubaccountSearchResult)!;
        ApiResponse<string> closed = JsonSerializer.Deserialize(Fixture.Read("Subaccounts/close-response.json"), Smtp2GoJsonContext.Default.ApiResponseString)!;
        ApiResponse<string> reopened = JsonSerializer.Deserialize(Fixture.Read("Subaccounts/reopen-response.json"), Smtp2GoJsonContext.Default.ApiResponseString)!;

        added.Data.Should().BeEquivalentTo(new Subaccount { Name = "Test Person", Id = "34l8oj", PlanSize = 10000, PlanUsed = 0, PlanRemaining = 10000, StateRaw = "Active", DedicatedIp = false, Archiving = true, Enforce2fa = true, SmsEnabled = true, SmsLimit = 1000 });
        added.Data.State.Should().Be(SubaccountState.Active);
        added.Data.Extra.Should().BeNull();

        searched.Data.ContinueToken.Should().BeEmpty();
        searched.Data.TotalCount.Should().Be(1);
        Subaccount row = searched.Data.Subaccounts.Should().ContainSingle().Which;
        row.Name.Should().Be("10000");
        row.Email.Should().Be("test@gmail.com");
        row.Id.Should().Be("GnlKn5");
        row.State.Should().Be(SubaccountState.Active);
        row.DedicatedIp.Should().BeFalse();
        row.SmsLimit.Should().BeNull();
        row.Extra.Should().BeNull();
        searched.Data.Extra.Should().BeNull();

        closed.Data.Should().Be("Successfully closed subaccount test@example.com");
        reopened.Data.Should().Be("Successfully reopened subaccount test@example.com");
    }

    [Fact]
    public void Unknown_states_are_readable_raw()
    {
        ApiResponse<Subaccount> added = JsonSerializer.Deserialize("""{"request_id":"r","data":{"id":"x","state":"Pending"}}""", Smtp2GoJsonContext.Default.ApiResponseSubaccount)!;

        added.Data.State.Should().Be(SubaccountState.Unknown);
        added.Data.StateRaw.Should().Be("Pending");
        JsonSerializer.Deserialize("""{"request_id":"r","data":{"state":"closed"}}""", Smtp2GoJsonContext.Default.ApiResponseSubaccount)!.Data.State.Should().Be(SubaccountState.Closed);
    }

    [Fact]
    public void Requests_are_validated()
    {
        List<string> errors = [];
        ((IRequestValidator)new SubaccountAddRequest { FullName = " ", Limit = 0, SmsLimit = -1 }).Validate(EndpointTable.Get("subaccount/add"), errors);
        errors.Should().Equal("fullname is required.", "limit must be positive.", "sms_limit must not be negative.");

        errors.Clear();
        ((IRequestValidator)new SubaccountEditRequest { Id = "" }).Validate(EndpointTable.Get("subaccount/edit"), errors);
        errors.Should().Equal("id is required.");

        errors.Clear();
        ((IRequestValidator)new SubaccountCloseRequest { Id = " " }).Validate(EndpointTable.Get("subaccount/close"), errors);
        ((IRequestValidator)new SubaccountReopenRequest { Id = "" }).Validate(EndpointTable.Get("subaccount/reopen"), errors);
        errors.Should().Equal("id is required.", "id is required.");

        errors.Clear();
        ((IRequestValidator)new SubaccountSearchRequest { PageSize = 0, SortDirection = SortDirection.Unknown, States = SubaccountStateFilter.Unknown }).Validate(EndpointTable.Get("subaccounts/search"), errors);
        errors.Should().Equal("page_size must be positive.", "sort_direction must not be SortDirection.Unknown.", "states must not be SubaccountStateFilter.Unknown.");
    }

    [Fact]
    public async Task Client_methods_post_to_the_documented_paths_and_ignore_the_subaccount_id()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler()
            .Respond("subaccounts/search", HttpStatusCode.OK, Fixture.Read("Subaccounts/search-response.json"))
            .Respond("subaccount/add", HttpStatusCode.OK, Fixture.Read("Subaccounts/add-response.json"))
            .Respond("subaccount/edit", HttpStatusCode.OK, Fixture.Read("Subaccounts/add-response.json"))
            .Respond("subaccount/close", HttpStatusCode.OK, Fixture.Read("Subaccounts/close-response.json"))
            .Respond("subaccount/reopen", HttpStatusCode.OK, Fixture.Read("Subaccounts/reopen-response.json"));
        Smtp2GoClient client = TestClient.Create(handler, o => o.DefaultSubaccountId = "sub-1");

        (await client.Subaccounts.SearchAsync(new SubaccountSearchRequest { SearchTerms = ["test"] })).Data.TotalCount.Should().Be(1);
        (await client.Subaccounts.AddAsync(new SubaccountAddRequest { FullName = "Test Person" })).Data.Id.Should().Be("34l8oj");
        (await client.Subaccounts.EditAsync(new SubaccountEditRequest { Id = "34l8oj", Limit = 20000 })).Data.Id.Should().Be("34l8oj");
        (await client.Subaccounts.CloseAsync(new SubaccountCloseRequest { Id = "34l8oj" })).Data.Should().StartWith("Successfully closed");
        (await client.Subaccounts.ReopenAsync(new SubaccountReopenRequest { Id = "34l8oj" })).Data.Should().StartWith("Successfully reopened");

        handler.Requests.Select(r => r.Endpoint!.Path).Should().Equal("subaccounts/search", "subaccount/add", "subaccount/edit", "subaccount/close", "subaccount/reopen");
        handler.Requests.Should().OnlyContain(r => !r.Body!.Contains("subaccount_id"), because: "none of the subaccount endpoints documents subaccount_id");
        handler.Requests[0].Body.Should().Be("""{"search_terms":["test"]}""");
        handler.Requests[2].Body.Should().Be("""{"id":"34l8oj","limit":20000}""");
    }

    [Fact]
    public async Task SearchAllAsync_follows_continue_tokens()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("subaccounts/search", async (request, ct) =>
        {
            string? token = JsonNode.Parse(await request.Content!.ReadAsStringAsync(ct))!["continue_token"]?.GetValue<string>();
            string page = token is null ? """{"continue_token":"next","subaccounts":[{"id":"a"},{"id":"b"}],"total_count":3}""" : """{"continue_token":"","subaccounts":[{"id":"c"}],"total_count":3}""";
            return FakeHttpMessageHandler.Json(HttpStatusCode.OK, $$"""{"request_id":"r","data":{{page}}}""");
        });
        Smtp2GoClient client = TestClient.Create(handler);

        List<string?> ids = [];
        await foreach (Subaccount subaccount in client.Subaccounts.SearchAllAsync(new SubaccountSearchRequest { PageSize = 2 }, cancellationToken: TestContext.Current.CancellationToken))
        {
            ids.Add(subaccount.Id);
        }

        ids.Should().Equal("a", "b", "c");
        handler.Requests.Should().HaveCount(2);
        handler.Requests.Should().OnlyContain(r => r.Body!.Contains("\"page_size\":2"));
    }

    [Fact]
    public async Task Null_requests_are_rejected()
    {
        Smtp2GoClient client = TestClient.Create(new FakeHttpMessageHandler());

        await ((Func<Task>)(() => client.Subaccounts.SearchAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        ((Action)(() => client.Subaccounts.SearchAllAsync(null!))).Should().Throw<ArgumentNullException>();
        await ((Func<Task>)(() => client.Subaccounts.AddAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.Subaccounts.EditAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.Subaccounts.CloseAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.Subaccounts.ReopenAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
    }
}
