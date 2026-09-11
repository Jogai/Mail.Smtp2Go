using System.Text.RegularExpressions;

namespace Scott.Mail.Smtp2Go.SpecHarvester;

/// <summary>One link from <c>llms.txt</c>.</summary>
/// <param name="Title">The link text.</param>
/// <param name="Url">The page URL (already ending in <c>.md</c>).</param>
/// <param name="Description">The text after the link, if any.</param>
public sealed record IndexEntry(string Title, Uri Url, string? Description)
{
    /// <summary>The last path segment without the <c>.md</c> suffix, for example <c>send-standard-email</c>.</summary>
    public string Slug
    {
        get
        {
            string last = Url.Segments[^1];
            return last.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ? last[..^3] : last;
        }
    }

    /// <summary>The path relative to the site root, for example <c>reference/send-standard-email.md</c>.</summary>
    public string RelativePath => Url.AbsolutePath.TrimStart('/');
}

/// <summary>Parses the <c>llms.txt</c> index published at the root of the developer site.</summary>
public static partial class LlmsIndex
{
    /// <summary>The relative path of the index.</summary>
    public const string RelativePath = "llms.txt";

    /// <summary>The overview page that documents the webhook callback payloads.</summary>
    public const string WebhooksOverviewPath = "docs/webhooks-overview.md";

    /// <summary>The changelog page.</summary>
    public const string ChangelogPath = "reference/changelog.md";

    /// <summary>Returns the links of the <c>## API Reference</c> section, in document order.</summary>
    public static IReadOnlyList<IndexEntry> ParseReferenceSection(string llmsText)
    {
        ArgumentNullException.ThrowIfNull(llmsText);
        List<IndexEntry> entries = [];
        bool inSection = false;
        foreach (string rawLine in llmsText.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                inSection = string.Equals(line.Trim(), "## API Reference", StringComparison.Ordinal);
                continue;
            }

            if (!inSection)
            {
                continue;
            }

            Match match = LinkLine().Match(line);
            if (!match.Success)
            {
                continue;
            }

            string? description = match.Groups["description"].Success ? match.Groups["description"].Value.Trim() : null;
            entries.Add(new IndexEntry(match.Groups["title"].Value.Trim(), new Uri(match.Groups["url"].Value), string.IsNullOrEmpty(description) ? null : description));
        }

        return entries;
    }

    [GeneratedRegex(@"^\s*-\s*\[(?<title>[^\]]+)\]\((?<url>https?://[^\s)]+)\)(?::\s*(?<description>.*))?$")]
    private static partial Regex LinkLine();
}
