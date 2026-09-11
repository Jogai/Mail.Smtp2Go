using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>One element of the <c>/email/batch</c> response: <see cref="EmailId"/> for an immediate send or <see cref="ScheduleId"/> for a scheduled one, in request order.</summary>
public sealed record EmailBatchItem
{
    /// <summary>The id of the sent email.</summary>
    [JsonPropertyName("email_id")]
    public string? EmailId { get; init; }

    /// <summary>The id of the queued email when the item carried a <c>schedule</c>.</summary>
    [JsonPropertyName("schedule_id")]
    public string? ScheduleId { get; init; }

    /// <summary>Any field this library does not model; the documentation for this endpoint is thin.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }

    /// <summary>Whether the email was queued for later delivery rather than sent.</summary>
    [JsonIgnore]
    public bool IsScheduled => !string.IsNullOrEmpty(ScheduleId);
}
