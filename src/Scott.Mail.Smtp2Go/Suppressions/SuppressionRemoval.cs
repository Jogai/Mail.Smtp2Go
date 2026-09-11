using System.Text.Json;
using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json;

namespace Scott.Mail.Smtp2Go;

/// <summary>One entry of <c>suppression/remove</c>'s result: whether the given reason was removed for the address.</summary>
public sealed record SuppressionRemoval
{
    /// <summary>The address or domain.</summary>
    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; init; }

    /// <summary>The <c>reason</c> exactly as returned.</summary>
    [JsonPropertyName("reason")]
    public string? ReasonRaw { get; init; }

    /// <summary><see cref="ReasonRaw"/> as a <see cref="SuppressionType"/>.</summary>
    [JsonIgnore]
    public SuppressionType Reason => ReasonRaw is not null && EnumNameTable<SuppressionType>.TryParse(ReasonRaw, out SuppressionType parsed) ? parsed : SuppressionType.Unknown;

    /// <summary>Whether a suppression of this reason existed and was removed.</summary>
    [JsonPropertyName("removed")]
    public bool? Removed { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
