using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>suppression/remove</c>: one entry per requested reason.</summary>
public sealed record SuppressionRemoveResult
{
    /// <summary>The outcome per reason.</summary>
    [JsonPropertyName("suppressions")]
    public IReadOnlyList<SuppressionRemoval>? Suppressions { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
