namespace Scott.Mail.Smtp2Go.Tests.Integration.Templates;

/// <summary>template/add, view, edit, search, delete against the sandbox. Skipped without a sandbox key; see <see cref="SandboxKey"/>.</summary>
[Trait("Category", "Sandbox")]
public class SandboxTemplateTests
{
    [Fact]
    public async Task Add_view_update_search_remove_round_trip()
    {
        Smtp2GoClient client = SandboxKey.CreateClient();
        CancellationToken ct = TestContext.Current.CancellationToken;
        string id = string.Concat("sm2go-", Guid.NewGuid().ToString("N").AsSpan(0, 12));
        string tag = string.Concat("sm2go-", Guid.NewGuid().ToString("N").AsSpan(0, 8));

        ApiResponse<Template> added = await client.Templates.AddAsync(new TemplateAddRequest
        {
            Id = id,
            TemplateName = "Scott.Mail.Smtp2Go integration",
            Subject = "Hello {{ name }}",
            HtmlBody = "<p>Hello {{ name }}</p>",
            TextBody = "Hello {{ name }}",
            TemplateVariables = new Dictionary<string, string> { ["name"] = "world" },
            Tags = [tag],
        }, cancellationToken: ct);
        try
        {
            added.Data.Id.Should().Be(id);

            ApiResponse<Template> viewed = await client.Templates.ViewAsync(id, cancellationToken: ct);
            viewed.Data.Id.Should().Be(id);
            viewed.Data.Subject.Should().Be("Hello {{ name }}");

            ApiResponse<Template> updated = await client.Templates.UpdateAsync(new TemplateUpdateRequest { Id = id, Subject = "Hi {{ name }}" }, cancellationToken: ct);
            updated.Data.Subject.Should().Be("Hi {{ name }}");

            ApiResponse<TemplateSearchResult> searched = await client.Templates.SearchAsync(new TemplateSearchRequest { Tags = [tag] }, cancellationToken: ct);
            searched.Data.Templates.Should().Contain(t => t.Id == id);
        }
        finally
        {
            ApiResponse<string> removed = await client.Templates.RemoveAsync(id, cancellationToken: ct);
            removed.RequestId.Should().NotBeNullOrWhiteSpace();
        }
    }
}
