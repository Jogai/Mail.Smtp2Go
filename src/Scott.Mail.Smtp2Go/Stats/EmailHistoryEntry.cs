using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>One row of <c>stats/email_history</c>. Which of <see cref="EmailAddress"/>, <see cref="Username"/>, <see cref="Domain"/> and <see cref="Subaccount"/> is set depends on <see cref="EmailHistoryRequest.GroupBy"/>.</summary>
public sealed record EmailHistoryEntry
{
    /// <summary>The sender address (<c>group_by: email_address</c>).</summary>
    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; init; }

    /// <summary>The SMTP user or API key (<c>group_by: username</c>).</summary>
    [JsonPropertyName("username")]
    public string? Username { get; init; }

    /// <summary>The user's description (<c>group_by: username</c>).</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The sender domain (<c>group_by: domain</c>).</summary>
    [JsonPropertyName("domain")]
    public string? Domain { get; init; }

    /// <summary>Whether the domain is verified (<c>group_by: domain</c>).</summary>
    [JsonPropertyName("domain_verified")]
    public bool? DomainVerified { get; init; }

    /// <summary>The subaccount name (<c>group_by: subaccount</c>).</summary>
    [JsonPropertyName("subaccount")]
    public string? Subaccount { get; init; }

    /// <summary>The last IP address that sent.</summary>
    [JsonPropertyName("lastip")]
    public string? LastIp { get; init; }

    /// <summary>Emails sent.</summary>
    [JsonPropertyName("used")]
    public long? Used { get; init; }

    /// <summary>Total bytes sent.</summary>
    [JsonPropertyName("bytecount")]
    public long? ByteCount { get; init; }

    /// <summary>Average email size in bytes.</summary>
    [JsonPropertyName("avgsize")]
    public double? AverageSize { get; init; }

    /// <summary>Bounces.</summary>
    [JsonPropertyName("bounces")]
    public long? Bounces { get; init; }

    /// <summary>Clicks.</summary>
    [JsonPropertyName("clicks")]
    public long? Clicks { get; init; }

    /// <summary>Opens.</summary>
    [JsonPropertyName("opens")]
    public long? Opens { get; init; }

    /// <summary>Rejects.</summary>
    [JsonPropertyName("rejects")]
    public long? Rejects { get; init; }

    /// <summary>Spam complaints.</summary>
    [JsonPropertyName("spam")]
    public long? Spam { get; init; }

    /// <summary>Unsubscribes.</summary>
    [JsonPropertyName("unsubscribes")]
    public long? Unsubscribes { get; init; }

    /// <summary>Bounce rate (a JSON number on this endpoint).</summary>
    [JsonPropertyName("bounce_percent")]
    public Percentage? BouncePercent { get; init; }

    /// <summary>Open rate.</summary>
    [JsonPropertyName("open_percent")]
    public Percentage? OpenPercent { get; init; }

    /// <summary>Reject rate.</summary>
    [JsonPropertyName("reject_percent")]
    public Percentage? RejectPercent { get; init; }

    /// <summary>Spam complaint rate.</summary>
    [JsonPropertyName("spam_percent")]
    public Percentage? SpamPercent { get; init; }

    /// <summary>Unsubscribe rate.</summary>
    [JsonPropertyName("unsubscribe_percent")]
    public Percentage? UnsubscribePercent { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
