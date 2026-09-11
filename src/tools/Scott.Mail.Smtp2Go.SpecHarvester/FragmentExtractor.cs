using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Scott.Mail.Smtp2Go.SpecHarvester;

/// <summary>What one reference page yielded.</summary>
/// <param name="Slug">The page slug.</param>
/// <param name="UpdatedAt">The <c>updatedAt</c> front-matter value, if present.</param>
/// <param name="Prose">The page text before the OpenAPI definition, without front matter and boilerplate.</param>
/// <param name="Spec">The parsed OpenAPI fragment, or <see langword="null"/> when the page has none or it did not parse.</param>
/// <param name="RawFragment">The fenced block text, when the page has one.</param>
/// <param name="Error">The parse error, when the block did not parse.</param>
public sealed record PageFragment(string Slug, string? UpdatedAt, string Prose, JsonObject? Spec, string? RawFragment, string? Error)
{
    /// <summary>Whether the page carried a fenced OpenAPI block at all.</summary>
    public bool HasFragment => RawFragment is not null;
}

/// <summary>Pulls the fenced <c>json</c> OpenAPI block out of a reference page.</summary>
public static partial class FragmentExtractor
{
    private const string Boilerplate = "Fetch the complete documentation index at:";

    /// <summary>Extracts the first fenced <c>json</c> block that starts with <c>{"openapi"</c> from <paramref name="markdown"/>.</summary>
    public static PageFragment Extract(string slug, string markdown)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentNullException.ThrowIfNull(markdown);

        string text = markdown.Replace("\r\n", "\n", StringComparison.Ordinal);
        string? updatedAt = null;
        string body = text;
        Match frontMatter = FrontMatter().Match(text);
        if (frontMatter.Success)
        {
            Match updated = UpdatedAt().Match(frontMatter.Groups["front"].Value);
            updatedAt = updated.Success ? updated.Groups["value"].Value.Trim() : null;
            body = text[frontMatter.Length..];
        }

        Match block = OpenApiBlock().Match(body);
        string prose = ExtractProse(block.Success ? body[..block.Index] : body);
        if (!block.Success)
        {
            return new PageFragment(slug, updatedAt, prose, null, null, null);
        }

        string raw = block.Groups["json"].Value;
        try
        {
            JsonNode? node = JsonNode.Parse(raw, documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            return node is JsonObject spec
                ? new PageFragment(slug, updatedAt, prose, spec, raw, null)
                : new PageFragment(slug, updatedAt, prose, null, raw, "The OpenAPI block is not a JSON object.");
        }
        catch (JsonException ex)
        {
            return new PageFragment(slug, updatedAt, prose, null, raw, ex.Message);
        }
    }

    /// <summary>Guesses the operation path from an unparsable block by looking for the first quoted <c>/family/op</c> string.</summary>
    public static string? GuessPath(string rawFragment)
    {
        ArgumentNullException.ThrowIfNull(rawFragment);
        Match match = QuotedPath().Match(rawFragment);
        return match.Success ? match.Groups["path"].Value.TrimStart('/') : null;
    }

    private static string ExtractProse(string text)
    {
        IEnumerable<string> lines = text.Split('\n')
            .Where(l => !l.StartsWith(Boilerplate, StringComparison.Ordinal))
            .Where(l => !string.Equals(l.Trim(), "# OpenAPI definition", StringComparison.Ordinal));
        return string.Join('\n', lines).Trim();
    }

    [GeneratedRegex(@"\A---\n(?<front>.*?)\n---\n", RegexOptions.Singleline)]
    private static partial Regex FrontMatter();

    [GeneratedRegex(@"^updatedAt:\s*(?<value>.+)$", RegexOptions.Multiline)]
    private static partial Regex UpdatedAt();

    [GeneratedRegex(@"```json[^\n]*\n(?<json>\s*\{\s*""openapi"".*?)\n```", RegexOptions.Singleline)]
    private static partial Regex OpenApiBlock();

    [GeneratedRegex(@"""(?<path>/[a-z0-9_]+(?:/[a-z0-9_\-]+)+)""")]
    private static partial Regex QuotedPath();
}
