using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>One delivery attempt of a processed email (<c>delivery_attempts[]</c> on <c>activity/search</c> results).</summary>
public sealed record DeliveryAttempt
{
    /// <summary>When the attempt was made (UTC).</summary>
    [JsonPropertyName("smtptime")]
    public DateTimeOffset? SmtpTime { get; init; }

    /// <summary>The host that made the attempt.</summary>
    [JsonPropertyName("host")]
    public string? Host { get; init; }

    /// <summary>The target server's response.</summary>
    [JsonPropertyName("smtpresponse")]
    public string? SmtpResponse { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
