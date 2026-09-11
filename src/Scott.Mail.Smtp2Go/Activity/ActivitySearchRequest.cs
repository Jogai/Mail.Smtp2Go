using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /activity/search</c> (rate-limited to 60 calls per minute). Every field is optional; the default range is today from midnight UTC.</summary>
public sealed record ActivitySearchRequest : IRequestValidator
{
    /// <summary>The maximum <see cref="Limit"/>.</summary>
    public const int MaxLimit = 1000;

    /// <summary>Range start, inclusive (UTC). Server default: today at midnight.</summary>
    [JsonPropertyName("start_date")]
    public DateTimeOffset? StartDate { get; init; }

    /// <summary>Range end, exclusive (UTC). Server default: now.</summary>
    [JsonPropertyName("end_date")]
    public DateTimeOffset? EndDate { get; init; }

    /// <summary>Match this text in any search field; separate alternatives with <c>|</c>.</summary>
    [JsonPropertyName("search")]
    public string? Search { get; init; }

    /// <summary>Events of one email, by its <c>email_id</c>.</summary>
    [JsonPropertyName("search_email_id")]
    public string? SearchEmailId { get; init; }

    /// <summary>Match in the subject.</summary>
    [JsonPropertyName("search_subject")]
    public string? SearchSubject { get; init; }

    /// <summary>Match in the sender.</summary>
    [JsonPropertyName("search_sender")]
    public string? SearchSender { get; init; }

    /// <summary>Match in the recipient.</summary>
    [JsonPropertyName("search_recipient")]
    public string? SearchRecipient { get; init; }

    /// <summary>Only emails sent by these SMTP users or API keys.</summary>
    [JsonPropertyName("search_usernames")]
    public IReadOnlyList<string>? SearchUsernames { get; init; }

    /// <summary>Only emails sent by these subaccount ids. This endpoint has no <c>subaccount_id</c> field; <see cref="RequestOptions.SubaccountId"/> is ignored for it.</summary>
    [JsonPropertyName("subaccounts")]
    public IReadOnlyList<string>? Subaccounts { get; init; }

    /// <summary>Page size, 1 to 1,000. Server default 100.</summary>
    [JsonPropertyName("limit")]
    public int? Limit { get; init; }

    /// <summary>The token from the previous page's <see cref="ActivitySearchResult.ContinueToken"/>. <see cref="IActivityClient.SearchAllAsync"/> manages it.</summary>
    [JsonPropertyName("continue_token")]
    public string? ContinueToken { get; init; }

    /// <summary>Only the most recent event of each email.</summary>
    [JsonPropertyName("only_latest")]
    public bool? OnlyLatest { get; init; }

    /// <summary>Only the most recent event of each email, ordered by sent date (overrides <see cref="OnlyLatest"/>).</summary>
    [JsonPropertyName("only_latest_by_sent")]
    public bool? OnlyLatestBySent { get; init; }

    /// <summary>Only these event types.</summary>
    [JsonPropertyName("event_types")]
    public IReadOnlyList<ActivityEventType>? EventTypes { get; init; }

    /// <summary>Include the full email headers in each event (<see cref="ActivityEvent.Headers"/>).</summary>
    [JsonPropertyName("include_headers")]
    public bool? IncludeHeaders { get; init; }

    /// <summary>Header names to extract into <see cref="ActivityEvent.CustomHeaders"/>.</summary>
    [JsonPropertyName("custom_headers")]
    public IReadOnlyList<string>? CustomHeaders { get; init; }

    /// <summary>Query another region's activity store (accounts with subaccounts in other regions). The API documents this as a free-form string.</summary>
    [JsonPropertyName("region")]
    public string? Region { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        if (Limit is { } limit && (limit < 1 || limit > MaxLimit))
        {
            errors.Add($"limit must be between 1 and {MaxLimit}.");
        }

        if (EventTypes is not null && EventTypes.Contains(ActivityEventType.Unknown))
        {
            errors.Add("event_types must not contain ActivityEventType.Unknown.");
        }
    }
}
