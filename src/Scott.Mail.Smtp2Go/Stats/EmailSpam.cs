using System.Text.Json.Serialization;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>stats/email_spam</c>: spam complaints and rejects for the last 30 days.</summary>
[Smtp2GoEndpoint("stats/email_spam")]
public sealed record EmailSpam
{
    /// <summary>Emails sent in the period.</summary>
    [JsonPropertyName("emails")]
    public long? Emails { get; init; }

    /// <summary>Rejected emails.</summary>
    [JsonPropertyName("rejects")]
    public long? Rejects { get; init; }

    /// <summary>Spam complaints.</summary>
    [JsonPropertyName("spams")]
    public long? Spams { get; init; }

    /// <summary>Spam complaint rate.</summary>
    [JsonPropertyName("spam_percent")]
    public Percentage? SpamPercent { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
