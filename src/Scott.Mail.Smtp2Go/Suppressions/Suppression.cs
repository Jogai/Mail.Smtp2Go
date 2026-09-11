using System.Text.Json;
using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json;

namespace Scott.Mail.Smtp2Go;

/// <summary>One suppressed address or domain from <c>suppression/view</c>.</summary>
public sealed record Suppression
{
    /// <summary>The suppressed address or domain.</summary>
    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; init; }

    /// <summary>The <c>reason</c> exactly as returned (<c>manual</c>, <c>spam</c>, ...).</summary>
    [JsonPropertyName("reason")]
    public string? ReasonRaw { get; init; }

    /// <summary><see cref="ReasonRaw"/> as a <see cref="SuppressionType"/>; <see cref="SuppressionType.Unknown"/> for values this library does not know.</summary>
    [JsonIgnore]
    public SuppressionType Reason => ReasonRaw is not null && EnumNameTable<SuppressionType>.TryParse(ReasonRaw, out SuppressionType parsed) ? parsed : SuppressionType.Unknown;

    /// <summary>The complaint text, where available.</summary>
    [JsonPropertyName("complaint")]
    public string? Complaint { get; init; }

    /// <summary>The description given when the suppression was added.</summary>
    [JsonPropertyName("block_description")]
    public string? BlockDescription { get; init; }

    /// <summary>The subject of the email that triggered the suppression, where available.</summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; init; }

    /// <summary>When the suppression was added (UTC).</summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
