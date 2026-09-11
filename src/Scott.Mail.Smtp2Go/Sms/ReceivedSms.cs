using System.Text.Json;
using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>One received SMS message. The docs example shows the numbers as JSON numbers; they are read as strings either way.</summary>
public sealed record ReceivedSms
{
    /// <summary>The number the message came from.</summary>
    [JsonPropertyName("source_address")]
    [JsonConverter(typeof(NumberOrStringConverter))]
    public string? SourceAddress { get; init; }

    /// <summary>The number the message was sent to.</summary>
    [JsonPropertyName("destination_address")]
    [JsonConverter(typeof(NumberOrStringConverter))]
    public string? DestinationAddress { get; init; }

    /// <summary>When the message arrived (UTC).</summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; init; }

    /// <summary>The message text.</summary>
    [JsonPropertyName("content")]
    public string? Content { get; init; }

    /// <summary>The message id.</summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; init; }

    /// <summary>The username (SMTP user or API key) the message is attributed to.</summary>
    [JsonPropertyName("username")]
    public string? Username { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
