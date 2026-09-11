using System.Text.Json.Serialization;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>stats/email_summary</c>: the cycle, bounce, spam and unsubscribe reports in one call (the docs note it may take longer).</summary>
[Smtp2GoEndpoint("stats/email_summary")]
public sealed record EmailSummary
{
    /// <summary>Start of the billing cycle (UTC).</summary>
    [JsonPropertyName("cycle_start")]
    public DateTimeOffset? CycleStart { get; init; }

    /// <summary>End of the billing cycle (UTC).</summary>
    [JsonPropertyName("cycle_end")]
    public DateTimeOffset? CycleEnd { get; init; }

    /// <summary>Emails sent this cycle.</summary>
    [JsonPropertyName("cycle_used")]
    public long? CycleUsed { get; init; }

    /// <summary>Emails left this cycle.</summary>
    [JsonPropertyName("cycle_remaining")]
    public long? CycleRemaining { get; init; }

    /// <summary>The cycle allowance.</summary>
    [JsonPropertyName("cycle_max")]
    public long? CycleMax { get; init; }

    /// <summary>Emails counted in the report.</summary>
    [JsonPropertyName("email_count")]
    public long? EmailCount { get; init; }

    /// <summary>Rejected emails (suppressed recipients, unverified senders, sandboxed credentials). Replaces the deprecated <see cref="BounceRejects"/> and <see cref="SpamRejects"/>.</summary>
    [JsonPropertyName("rejects")]
    public long? Rejects { get; init; }

    /// <summary>Deprecated by SMTP2GO; use <see cref="Rejects"/>.</summary>
    [JsonPropertyName("bounce_rejects")]
    [Obsolete(ObsoleteMessages.StatsRejects)]
    public long? BounceRejects { get; init; }

    /// <summary>Deprecated by SMTP2GO; use <see cref="Rejects"/>.</summary>
    [JsonPropertyName("spam_rejects")]
    [Obsolete(ObsoleteMessages.StatsRejects)]
    public long? SpamRejects { get; init; }

    /// <summary>Hard bounces.</summary>
    [JsonPropertyName("hardbounces")]
    public long? HardBounces { get; init; }

    /// <summary>Soft bounces.</summary>
    [JsonPropertyName("softbounces")]
    public long? SoftBounces { get; init; }

    /// <summary>Bounce rate, as the API's string percentage.</summary>
    [JsonPropertyName("bounce_percent")]
    public Percentage? BouncePercent { get; init; }

    /// <summary>Spam complaints.</summary>
    [JsonPropertyName("spam_emails")]
    public long? SpamEmails { get; init; }

    /// <summary>Spam complaint rate.</summary>
    [JsonPropertyName("spam_percent")]
    public Percentage? SpamPercent { get; init; }

    /// <summary>Unsubscribes.</summary>
    [JsonPropertyName("unsubscribes")]
    public long? Unsubscribes { get; init; }

    /// <summary>Unsubscribe rate.</summary>
    [JsonPropertyName("unsubscribe_percent")]
    public Percentage? UnsubscribePercent { get; init; }

    /// <summary>Opens.</summary>
    [JsonPropertyName("opens")]
    public long? Opens { get; init; }

    /// <summary>Clicks.</summary>
    [JsonPropertyName("clicks")]
    public long? Clicks { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
