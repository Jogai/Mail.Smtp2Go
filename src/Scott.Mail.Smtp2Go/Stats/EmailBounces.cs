using System.Text.Json.Serialization;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>stats/email_bounces</c>: bounces and rejects for the last 30 days.</summary>
[Smtp2GoEndpoint("stats/email_bounces")]
public sealed record EmailBounces
{
    /// <summary>Emails sent in the period.</summary>
    [JsonPropertyName("emails")]
    public long? Emails { get; init; }

    /// <summary>Rejected emails.</summary>
    [JsonPropertyName("rejects")]
    public long? Rejects { get; init; }

    /// <summary>Soft bounces.</summary>
    [JsonPropertyName("softbounces")]
    public long? SoftBounces { get; init; }

    /// <summary>Hard bounces.</summary>
    [JsonPropertyName("hardbounces")]
    public long? HardBounces { get; init; }

    /// <summary>Bounce rate.</summary>
    [JsonPropertyName("bounce_percent")]
    public Percentage? BouncePercent { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
