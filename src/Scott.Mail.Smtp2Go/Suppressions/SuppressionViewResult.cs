using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>suppression/view</c>: one page of suppressions.</summary>
public sealed record SuppressionViewResult
{
    /// <summary>The suppressions of this page.</summary>
    [JsonPropertyName("results")]
    public IReadOnlyList<Suppression>? Results { get; init; }

    /// <summary>Pass as <see cref="SuppressionViewRequest.ContinueToken"/> for the next page; <see langword="null"/> on the last page.</summary>
    [JsonPropertyName("continue_token")]
    public string? ContinueToken { get; init; }

    /// <summary>The number of suppressions matching the filters.</summary>
    [JsonPropertyName("total_results")]
    public long? TotalResults { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
