using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /email/scheduled/search</c>. Every field is optional; without filters the whole queue is listed, <see cref="Limit"/> (server default 1,000) at a time.</summary>
public sealed record ScheduledEmailSearchRequest
{
    /// <summary>Find one queued email by the <c>schedule_id</c> returned from <c>/email/send</c> or <c>/email/mime</c>.</summary>
    [JsonPropertyName("schedule_id")]
    public string? ScheduleId { get; init; }

    /// <summary>Match on subject.</summary>
    [JsonPropertyName("search_subject")]
    public string? SearchSubject { get; init; }

    /// <summary>Match on recipient.</summary>
    [JsonPropertyName("search_recipient")]
    public string? SearchRecipient { get; init; }

    /// <summary>Match on sender.</summary>
    [JsonPropertyName("search_sender")]
    public string? SearchSender { get; init; }

    /// <summary>Page size. Server default 1,000.</summary>
    [JsonPropertyName("limit")]
    public int? Limit { get; init; }

    /// <summary>1-based page number.</summary>
    [JsonPropertyName("page")]
    public int? Page { get; init; }
}
