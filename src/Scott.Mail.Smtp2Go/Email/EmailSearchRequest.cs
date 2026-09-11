using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of the deprecated <c>POST /email/search</c>. SMTP2GO will remove the endpoint in a future API version; use the activity search instead.</summary>
[Obsolete(ObsoleteMessages.EmailSearch)]
public sealed record EmailSearchRequest
{
    /// <summary>Start of the window (UTC). Server default: today at midnight.</summary>
    [JsonPropertyName("start_date")]
    public DateTimeOffset? StartDate { get; init; }

    /// <summary>End of the window (UTC). Server default: now.</summary>
    [JsonPropertyName("end_date")]
    public DateTimeOffset? EndDate { get; init; }

    /// <summary>Maximum emails to return, 1 to 5,000. Server default 5,000.</summary>
    [JsonPropertyName("limit")]
    public int? Limit { get; init; }

    /// <summary>Also return counts per status.</summary>
    [JsonPropertyName("status_counts")]
    public bool? StatusCounts { get; init; }

    /// <summary>Only emails that were opened (needs open tracking).</summary>
    [JsonPropertyName("opened_only")]
    public bool? OpenedOnly { get; init; }

    /// <summary>Only emails with a clicked link (needs click tracking).</summary>
    [JsonPropertyName("clicked_only")]
    public bool? ClickedOnly { get; init; }

    /// <summary>Case-insensitive string matching.</summary>
    [JsonPropertyName("ignore_case")]
    public bool? IgnoreCase { get; init; }

    /// <summary>The documented filter query syntax.</summary>
    [JsonPropertyName("filter_query")]
    public string? FilterQuery { get; init; }

    /// <summary>Only these email ids.</summary>
    [JsonPropertyName("email_id")]
    public IReadOnlyList<string>? EmailId { get; init; }

    /// <summary>Only emails sent by this SMTP user.</summary>
    [JsonPropertyName("username")]
    public string? Username { get; init; }

    /// <summary>Header names whose values should be returned with each email.</summary>
    [JsonPropertyName("headers")]
    public IReadOnlyList<string>? Headers { get; init; }

    /// <summary>The token from a previous result to fetch the next page.</summary>
    [JsonPropertyName("continue_token")]
    public string? ContinueToken { get; init; }
}
