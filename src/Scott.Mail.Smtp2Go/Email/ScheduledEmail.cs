using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>One queued email from <c>/email/scheduled/search</c>.</summary>
public sealed record ScheduledEmail
{
    /// <summary>The queue id; pass it to <see cref="IEmailClient.RemoveScheduledAsync"/> to cancel delivery.</summary>
    [JsonPropertyName("schedule_id")]
    public string? ScheduleId { get; init; }

    /// <summary>When the email will be sent.</summary>
    [JsonPropertyName("schedule")]
    public DateTimeOffset? Schedule { get; init; }

    /// <summary>The sender.</summary>
    [JsonPropertyName("sender")]
    public string? Sender { get; init; }

    /// <summary>The subject.</summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; init; }

    /// <summary>The recipients, as one string exactly as the API returns it.</summary>
    [JsonPropertyName("recipients")]
    public string? Recipients { get; init; }

    /// <summary>The IP address the email was scheduled from.</summary>
    [JsonPropertyName("client_ip")]
    public string? ClientIp { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
