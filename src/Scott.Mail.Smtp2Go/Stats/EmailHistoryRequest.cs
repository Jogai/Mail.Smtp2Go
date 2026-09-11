using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /stats/email_history</c>. Every field is optional; the default range is the last 30 days grouped by sender address.</summary>
[Smtp2GoEndpoint("stats/email_history")]
public sealed record EmailHistoryRequest
{
    /// <summary>Row grouping. Server default: <see cref="EmailHistoryGroupBy.EmailAddress"/>.</summary>
    [JsonPropertyName("group_by")]
    public EmailHistoryGroupBy? GroupBy { get; init; }

    /// <summary>Range start (UTC). Server default: 30 days before today at midnight.</summary>
    [JsonPropertyName("start_date")]
    public DateTimeOffset? StartDate { get; init; }

    /// <summary>Range end (UTC). Server default: now.</summary>
    [JsonPropertyName("end_date")]
    public DateTimeOffset? EndDate { get; init; }

    /// <summary>Restrict the report to these subaccount ids (from <c>subaccounts/search</c>).</summary>
    [JsonPropertyName("subaccounts")]
    public IReadOnlyList<string>? Subaccounts { get; init; }
}
