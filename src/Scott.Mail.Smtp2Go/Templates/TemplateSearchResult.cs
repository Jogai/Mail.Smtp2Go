using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>template/search</c>: one page of templates (id, name, subject, tags, last_updated; no bodies).</summary>
public sealed record TemplateSearchResult
{
    /// <summary>The templates of this page.</summary>
    [JsonPropertyName("templates")]
    public IReadOnlyList<Template>? Templates { get; init; }

    /// <summary>Pass as <see cref="TemplateSearchRequest.ContinueToken"/> for the next page; <see langword="null"/> on the last page.</summary>
    [JsonPropertyName("continue_token")]
    public string? ContinueToken { get; init; }

    /// <summary>The number of templates matching the search.</summary>
    [JsonPropertyName("total_count")]
    public long? TotalCount { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
