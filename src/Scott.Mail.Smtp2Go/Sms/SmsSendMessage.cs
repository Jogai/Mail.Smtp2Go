using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>One message of an <c>sms/send</c> call.</summary>
public sealed record SmsSendMessage
{
    /// <summary>The destination number.</summary>
    [JsonPropertyName("destination")]
    public string? Destination { get; init; }

    /// <summary>The message id, also carried by the SMS webhook events.</summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; init; }

    /// <summary>The status at the time of the response.</summary>
    [JsonPropertyName("status")]
    public SmsStatus? Status { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
