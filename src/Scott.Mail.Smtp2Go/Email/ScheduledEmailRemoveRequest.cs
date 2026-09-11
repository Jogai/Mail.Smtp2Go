using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /email/scheduled/remove</c>. <see cref="IEmailClient.RemoveScheduledAsync"/> builds it from a bare id.</summary>
[Transport.Smtp2GoEndpoint("email/scheduled/remove")]
public sealed record ScheduledEmailRemoveRequest
{
    /// <summary>The <c>schedule_id</c> from <c>/email/send</c>, <c>/email/mime</c>, <c>/email/batch</c> or <c>/email/scheduled/search</c>.</summary>
    [JsonPropertyName("schedule_id")]
    public required string ScheduleId { get; init; }
}
