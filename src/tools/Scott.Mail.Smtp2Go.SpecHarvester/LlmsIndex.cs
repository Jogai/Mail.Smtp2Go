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

    private const string ReferenceHeading = "## API Reference";

    private static bool IsReferenceHeading(string line)
    {
        string heading = line.Trim();
        return string.Equals(heading, ReferenceHeading, StringComparison.Ordinal) || heading.StartsWith(ReferenceHeading + ":", StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns the links under every API reference heading, in document order. The index had one <c>## API Reference</c> section until
    /// September 2026 and one <c>## API Reference: GROUP</c> section per endpoint family since; both forms are read.
    /// </summary>
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
                inSection = IsReferenceHeading(line);
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
