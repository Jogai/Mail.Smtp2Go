using System.Text.Json;
using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>A configured webhook, as returned by every <c>webhook/*</c> operation.</summary>
public sealed record Webhook
{
    /// <summary>The webhook id; pass it to <see cref="IWebhookClient.EditAsync"/> and <see cref="IWebhookClient.RemoveAsync"/>.</summary>
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    /// <summary>The URL SMTP2GO posts callbacks to.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    /// <summary>The email events the webhook receives.</summary>
    [JsonPropertyName("events")]
    public IReadOnlyList<WebhookEmailEvent>? Events { get; init; }

    /// <summary>The SMS events the webhook receives.</summary>
    [JsonPropertyName("sms_events")]
    public IReadOnlyList<WebhookSmsEvent>? SmsEvents { get; init; }

    /// <summary>Custom email headers included in callbacks. The docs example shows a single string here; both a string and an array are read.</summary>
    [JsonPropertyName("headers")]
    [JsonConverter(typeof(StringOrStringArrayConverter))]
    public IReadOnlyList<string>? Headers { get; init; }

    /// <summary>The SMTP users, API keys or authenticated IPs the webhook covers; empty means all. Read as a string or an array, like <see cref="Headers"/>.</summary>
    [JsonPropertyName("usernames")]
    [JsonConverter(typeof(StringOrStringArrayConverter))]
    public IReadOnlyList<string>? Usernames { get; init; }

    /// <summary>The callback body encoding.</summary>
    [JsonPropertyName("output_format")]
    public WebhookOutputFormat? OutputFormat { get; init; }

    /// <summary>The <c>Authorization</c> scheme added to callbacks, if any.</summary>
    [JsonPropertyName("auth_header_type")]
    public WebhookAuthHeaderType? AuthHeaderType { get; init; }

    /// <summary>The <c>Authorization</c> value: <c>base64(user:pass)</c> for <see cref="WebhookAuthHeaderType.Basic"/>, a token for <see cref="WebhookAuthHeaderType.Bearer"/>.</summary>
    [JsonPropertyName("auth_header_value")]
    public string? AuthHeaderValue { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
