using System.Text.Json;
using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>One sent SMS message from <c>sms/view-sent</c>.</summary>
public sealed record SentSms
{
    /// <summary>The message id.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>When the message was sent (UTC).</summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; init; }

    /// <summary>The username (SMTP user or API key) that sent it.</summary>
    [JsonPropertyName("username")]
    public string? Username { get; init; }

    /// <summary>The sender used, for example <c>shared</c> or a dedicated number.</summary>
    [JsonPropertyName("sender")]
    public string? Sender { get; init; }

    /// <summary>The sender's email address when the message was sent via email.</summary>
    [JsonPropertyName("sender_email")]
    public string? SenderEmail { get; init; }

    /// <summary>The number the message was sent to.</summary>
    [JsonPropertyName("destination_address")]
    [JsonConverter(typeof(NumberOrStringConverter))]
    public string? DestinationAddress { get; init; }

    /// <summary>The country of the destination number, for example <c>US</c>.</summary>
    [JsonPropertyName("destination_address_country")]
    public string? DestinationAddressCountry { get; init; }

    /// <summary>The message format, for example <c>SMS</c>.</summary>
    [JsonPropertyName("format")]
    public string? Format { get; init; }

    /// <summary>The delivery status as free text, for example <c>Message discarded</c>.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; init; }

    /// <summary>The message text.</summary>
    [JsonPropertyName("content")]
    public string? Content { get; init; }

    /// <summary>The SMS units the message used.</summary>
    [JsonPropertyName("units")]
    public int? Units { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
