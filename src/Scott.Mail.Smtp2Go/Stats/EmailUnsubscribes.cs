using System.Text.Json.Serialization;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>stats/email_unsubs</c>: unsubscribes and rejects for the last 30 days.</summary>
[Smtp2GoEndpoint("stats/email_unsubs")]
public sealed record EmailUnsubscribes
{
    /// <summary>Emails sent in the period.</summary>
    [JsonPropertyName("emails")]
    public long? Emails { get; init; }

    /// <summary>Rejected emails.</summary>
    [JsonPropertyName("rejects")]
    public long? Rejects { get; init; }

    /// <summary>Unsubscribes.</summary>
    [JsonPropertyName("unsubscribes")]
    public long? Unsubscribes { get; init; }

    /// <summary>Unsubscribe rate.</summary>
    [JsonPropertyName("unsubscribe_percent")]
    public Percentage? UnsubscribePercent { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
