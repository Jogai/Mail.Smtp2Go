using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Tests.Unit.Email;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Templates;

public class TemplateTests
{
    private static readonly TemplateAddRequest s_fullAdd = new()
    {
        TemplateName = "Order receipt",
        Id = "order-receipt",
        Subject = "Order receipt for {{ product_name }}",
        HtmlBody = "<p>Thanks for buying {{ product_name }}</p>",
        TextBody = "Thanks for buying {{ product_name }}",
        TemplateVariables = new Dictionary<string, string> { ["product_name"] = "Widget" },
        Tags = ["orders", "transactional"],
    };

    [Fact]
    public void Template_family_is_seeded_with_the_documented_paths()
    {
        IEnumerable<Endpoint> templates = EndpointTable.All.Where(e => e.Path.StartsWith("template/", StringComparison.Ordinal));

        templates.Select(e => e.Path).Should().BeEquivalentTo("template/add", "template/edit", "template/delete", "template/search", "template/view");
        templates.Should().OnlyContain(e => !e.AcceptsSubaccountId && e.RateLimit == RateLimitClass.None);
        templates.Where(e => e.Idempotent).Select(e => e.Path).Should().BeEquivalentTo("template/search", "template/view");
    }

    [Fact]
    public void Add_request_serialises_every_documented_field()
    {
        Golden.AssertMatchesFixture(JsonSerializer.Serialize(s_fullAdd, Smtp2GoJsonContext.Default.TemplateAddRequest), "Templates/add-request-full.json");
    }

    [Fact]
    public void Update_request_serialises_every_documented_field()
    {
        TemplateUpdateRequest request = new()
        {
            Id = "order-receipt",
            NewId = "order-receipt-v2",
            TemplateName = "Order receipt v2",
            Subject = "Your receipt",
            HtmlBody = "<p>v2</p>",
            TextBody = "v2",
            TemplateVariables = new Dictionary<string, string> { ["product_name"] = "Gadget" },
            Tags = ["orders"],
        };

        Golden.AssertMatchesFixture(JsonSerializer.Serialize(request, Smtp2GoJsonContext.Default.TemplateUpdateRequest), "Templates/update-request-full.json");
    }

    [Fact]
    public void Search_request_serialises_every_filter()
    {
        TemplateSearchRequest request = new()
        {
            FuzzySearch = true,
            SearchTerms = ["receipt", "order"],
            Tags = ["orders"],
            SortDirection = SortDirection.Desc,
            PageSize = 50,
            ContinueToken = "eyJwYWdlIjoyfQ==",
        };

        Golden.AssertMatchesFixture(JsonSerializer.Serialize(request, Smtp2GoJsonContext.Default.TemplateSearchRequest), "Templates/search-request-full.json");
        JsonSerializer.Serialize(new TemplateSearchRequest(), Smtp2GoJsonContext.Default.TemplateSearchRequest).Should().Be("{}");
    }

    [Fact]
    public void Docs_add_and_edit_responses_deserialise_with_an_empty_extra()
    {
        ApiResponse<Template> added = JsonSerializer.Deserialize(Fixture.Read("Templates/add-response.json"), Smtp2GoJsonContext.Default.ApiResponseTemplate)!;
        ApiResponse<Template> edited = JsonSerializer.Deserialize(Fixture.Read("Templates/edit-response.json"), Smtp2GoJsonContext.Default.ApiResponseTemplate)!;

        added.Data.TemplateName.Should().Be("Shiny new name");
        added.Data.DisplayName.Should().Be("Shiny new name");
        added.Data.Name.Should().BeNull();
        added.Data.Id.Should().Be("testid");
        added.Data.Subject.Should().Be("Shiny new Subject");
        added.Data.HtmlBody.Should().Be("Shiny HTML body. This is a {{ variable }}");
        added.Data.TextBody.Should().Be("Shiny Text body");
        added.Data.TemplateVariables.Should().Equal(new Dictionary<string, string> { ["variable"] = "strawberries" });
        added.Data.Tags.Should().Equal("tagged");
        added.Data.LastUpdated.Should().BeNull();
        added.Data.Extra.Should().BeNull();
        edited.Data.Id.Should().Be("newid");
        edited.Data.Extra.Should().BeNull();
    }

    [Fact]
    public void Docs_search_and_view_responses_deserialise_with_an_empty_extra()
    {
        ApiResponse<TemplateSearchResult> search = JsonSerializer.Deserialize(Fixture.Read("Templates/search-response.json"), Smtp2GoJsonContext.Default.ApiResponseTemplateSearchResult)!;
        ApiResponse<Template> view = JsonSerializer.Deserialize(Fixture.Read("Templates/view-response.json"), Smtp2GoJsonContext.Default.ApiResponseTemplate)!;

        search.Data.ContinueToken.Should().BeNull();
        search.Data.TotalCount.Should().Be(1);
        Template listed = search.Data.Templates.Should().ContainSingle().Which;
        listed.Name.Should().Be("Order receipt");
        listed.DisplayName.Should().Be("Order receipt");
        listed.Id.Should().Be("5355878");
        listed.Tags.Should().Equal("one", "two", "five", "four");
        listed.LastUpdated.Should().Be(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));
        listed.Extra.Should().BeNull();
        search.Data.Extra.Should().BeNull();
        view.Data.Name.Should().Be("Shiny new name");
        view.Data.HtmlBody.Should().BeEmpty();
        view.Data.TemplateVariables.Should().ContainKey("variable");
        view.Data.LastUpdated.Should().NotBeNull();
        view.Data.Extra.Should().BeNull();
    }

    [Fact]
    public void Docs_delete_response_is_a_string()
    {
        ApiResponse<string> response = JsonSerializer.Deserialize(Fixture.Read("Templates/delete-response.json"), Smtp2GoJsonContext.Default.ApiResponseString)!;

        response.Data.Should().Be("Successfully deleted template 'example'");
    }

    [Fact]
    public void Length_limits_and_required_fields_are_validated()
    {
        List<string> errors = [];
        ((IRequestValidator)new TemplateAddRequest { TemplateName = new string('n', 65), Id = "abcd", Subject = "", HtmlBody = null!, TextBody = null! }).Validate(EndpointTable.Get("template/add"), errors);
        errors.Should().Equal("template_name must be 1 to 64 characters.", "id must be 5 to 24 characters.", "subject is required.", "html_body is required.", "text_body is required.");

        errors.Clear();
        ((IRequestValidator)new TemplateUpdateRequest { Id = "", NewId = new string('x', 25), TemplateName = "" }).Validate(EndpointTable.Get("template/edit"), errors);
        errors.Should().Equal("id is required.", "new_id must be 5 to 24 characters.", "template_name must be 1 to 64 characters.");

        errors.Clear();
        ((IRequestValidator)new TemplateSearchRequest { PageSize = 0, SortDirection = SortDirection.Unknown }).Validate(EndpointTable.Get("template/search"), errors);
        errors.Should().Equal("page_size must be positive.", "sort_direction must not be SortDirection.Unknown.");

        errors.Clear();
        ((IRequestValidator)s_fullAdd).Validate(EndpointTable.Get("template/add"), errors);
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Client_methods_post_to_the_documented_paths()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler()
            .Respond("template/add", HttpStatusCode.OK, Fixture.Read("Templates/add-response.json"))
            .Respond("template/edit", HttpStatusCode.OK, Fixture.Read("Templates/edit-response.json"))
            .Respond("template/delete", HttpStatusCode.OK, Fixture.Read("Templates/delete-response.json"))
            .Respond("template/search", HttpStatusCode.OK, Fixture.Read("Templates/search-response.json"))
            .Respond("template/view", HttpStatusCode.OK, Fixture.Read("Templates/view-response.json"));
        Smtp2GoClient client = TestClient.Create(handler);

        (await client.Templates.AddAsync(s_fullAdd)).Data.Id.Should().Be("testid");
        (await client.Templates.UpdateAsync(new TemplateUpdateRequest { Id = "testid", NewId = "newid" })).Data.Id.Should().Be("newid");
        (await client.Templates.RemoveAsync("example")).Data.Should().StartWith("Successfully deleted");
        (await client.Templates.SearchAsync(new TemplateSearchRequest { Tags = ["one"] })).Data.TotalCount.Should().Be(1);
        (await client.Templates.ViewAsync("testid")).Data.Name.Should().Be("Shiny new name");

        handler.Requests.Select(r => r.Endpoint!.Path).Should().Equal("template/add", "template/edit", "template/delete", "template/search", "template/view");
        Golden.AssertMatchesFixture(handler.Requests[0].Body!, "Templates/add-request-full.json");
        handler.Requests[1].Body.Should().Be("""{"id":"testid","new_id":"newid"}""");
        handler.Requests[2].Body.Should().Be("""{"id":"example"}""");
        handler.Requests[3].Body.Should().Be("""{"tags":["one"]}""");
        handler.Requests[4].Body.Should().Be("""{"id":"testid"}""");
    }

    [Fact]
    public async Task SearchAllAsync_follows_continue_tokens()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("template/search", async (request, ct) =>
        {
            string? token = JsonNode.Parse(await request.Content!.ReadAsStringAsync(ct))!["continue_token"]?.GetValue<string>();
            string page = token is null ? """{"templates":[{"id":"t1"},{"id":"t2"}],"continue_token":"next","total_count":3}""" : """{"templates":[{"id":"t3"}],"continue_token":null,"total_count":3}""";
            return FakeHttpMessageHandler.Json(HttpStatusCode.OK, $$"""{"request_id":"r","data":{{page}}}""");
        });
        Smtp2GoClient client = TestClient.Create(handler);

        List<string?> ids = [];
        await foreach (Template template in client.Templates.SearchAllAsync(new TemplateSearchRequest { PageSize = 2 }, cancellationToken: TestContext.Current.CancellationToken))
        {
            ids.Add(template.Id);
        }

        ids.Should().Equal("t1", "t2", "t3");
        handler.Requests.Should().HaveCount(2);
        handler.Requests.Should().OnlyContain(r => r.Body!.Contains("\"page_size\":2"));
    }

    [Fact]
    public async Task Null_and_blank_arguments_are_rejected()
    {
        Smtp2GoClient client = TestClient.Create(new FakeHttpMessageHandler());

        await ((Func<Task>)(() => client.Templates.AddAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.Templates.UpdateAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.Templates.SearchAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.Templates.RemoveAsync(" "))).Should().ThrowAsync<ArgumentException>();
        await ((Func<Task>)(() => client.Templates.ViewAsync(""))).Should().ThrowAsync<ArgumentException>();
        ((Action)(() => client.Templates.SearchAllAsync(null!))).Should().Throw<ArgumentNullException>();
    }
}
