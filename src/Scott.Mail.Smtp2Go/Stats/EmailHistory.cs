using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>stats/email_history</c>: per-sender, per-user, per-domain or per-subaccount totals over a date range.</summary>
public sealed record EmailHistory
{
    /// <summary>Number of rows.</summary>
    [JsonPropertyName("count")]
    public long? Count { get; init; }

    /// <summary>The rows.</summary>
    [JsonPropertyName("history")]
    public IReadOnlyList<EmailHistoryEntry>? History { get; init; }

    /// <summary>Overall bounce rate.</summary>
    [JsonPropertyName("bounce_percent_total")]
    public Percentage? BouncePercentTotal { get; init; }

    /// <summary>Overall open rate.</summary>
    [JsonPropertyName("open_percent_total")]
    public Percentage? OpenPercentTotal { get; init; }

    /// <summary>Overall reject rate.</summary>
    [JsonPropertyName("reject_percent_total")]
    public Percentage? RejectPercentTotal { get; init; }

    /// <summary>Overall spam complaint rate.</summary>
    [JsonPropertyName("spam_percent_total")]
    public Percentage? SpamPercentTotal { get; init; }

    /// <summary>Overall unsubscribe rate.</summary>
    [JsonPropertyName("unsubscribe_percent_total")]
    public Percentage? UnsubscribePercentTotal { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
