using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>suppression/add</c>.</summary>
public sealed record SuppressionAddResult
{
    /// <summary>Whether the suppression was added.</summary>
    [JsonPropertyName("added")]
    public bool? Added { get; init; }

    /// <summary>The description stored.</summary>
    [JsonPropertyName("block_description")]
    public string? BlockDescription { get; init; }

    /// <summary>The suppressed address or domain.</summary>
    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
