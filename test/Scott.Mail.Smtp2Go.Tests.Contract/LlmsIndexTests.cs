using Scott.Mail.Smtp2Go.SpecHarvester;

namespace Scott.Mail.Smtp2Go.Tests.Contract;

/// <summary>The harvester finds the reference pages in both shapes the developer site has published for <c>llms.txt</c>.</summary>
public class LlmsIndexTests
{
    private const string SingleSection = """
        # SMTP2GO-API-Docs Documentation

        ## Guides
        - [Introduction](https://developers.smtp2go.com/docs/introduction-guide.md)

        ## API Reference
        - [Changelog](https://developers.smtp2go.com/reference/changelog.md)
        - [Send a standard email](https://developers.smtp2go.com/reference/send-standard-email.md): Send an email by passing a JSON email object
        """;

    private const string GroupedSections = """
        # SMTP2GO-API-Docs Documentation

        ## Guides: Welcome
        - [Introduction](https://developers.smtp2go.com/docs/introduction-guide.md)

        ## API Reference: SMTP2GO API v3.0.4
        - [Changelog](https://developers.smtp2go.com/reference/changelog.md)

        ## API Reference: EMAILS
        - [Send a standard email](https://developers.smtp2go.com/reference/send-standard-email.md): Send an email by passing a JSON email object

        ## Guides: Webhooks
        - [Webhooks Overview](https://developers.smtp2go.com/docs/webhooks-overview.md)
        """;

    [Theory]
    [InlineData(SingleSection)]
    [InlineData(GroupedSections)]
    public void Reference_links_are_found_under_single_and_grouped_headings(string index)
    {
        IReadOnlyList<IndexEntry> entries = LlmsIndex.ParseReferenceSection(index);

        entries.Select(e => e.Slug).Should().Equal("changelog", "send-standard-email");
        entries[1].Description.Should().Be("Send an email by passing a JSON email object");
    }

    [Fact]
    public void Headings_that_only_start_alike_are_not_reference_sections()
    {
        IReadOnlyList<IndexEntry> entries = LlmsIndex.ParseReferenceSection("""
            ## API Reference Notes
            - [Not an endpoint](https://developers.smtp2go.com/docs/notes.md)
            """);

        entries.Should().BeEmpty();
    }
}
