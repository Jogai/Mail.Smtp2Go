using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The body of <c>POST /webhook/add</c>. Only <see cref="Url"/> is required. <c>subaccount_id</c> is not a property: set it through
/// <see cref="RequestOptions.SubaccountId"/> or <see cref="Smtp2GoClientOptions.DefaultSubaccountId"/> and the transport merges it in.
/// </summary>
public sealed record WebhookAddRequest : IRequestValidator
{
    /// <summary>The URL SMTP2GO posts callbacks to. Credentials in the URL (<c>https://user:pass@host/path</c>) are the documented alternative to <see cref="AuthHeaderType"/>.</summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>The email events to receive. Omit for none.</summary>
    [JsonPropertyName("events")]
    public IReadOnlyList<WebhookEmailEvent>? Events { get; init; }

    /// <summary>The SMS events to receive. Omit for none.</summary>
    [JsonPropertyName("sms_events")]
    public IReadOnlyList<WebhookSmsEvent>? SmsEvents { get; init; }

    /// <summary>Custom email headers to include in callbacks. They must exist in the sent emails; <c>Subject</c> and <c>Message-Id</c> are always included.</summary>
    [JsonPropertyName("headers")]
    public IReadOnlyList<string>? Headers { get; init; }

    /// <summary>Restrict the webhook to these SMTP users, API keys or authenticated IPs. Omit for all.</summary>
    [JsonPropertyName("usernames")]
    public IReadOnlyList<string>? Usernames { get; init; }

    /// <summary>The callback body encoding. The server defaults to <see cref="WebhookOutputFormat.Form"/> when omitted; <see cref="WebhookOutputFormat.Json"/> is recommended.</summary>
    [JsonPropertyName("output_format")]
    public WebhookOutputFormat? OutputFormat { get; init; }

    /// <summary>The <c>Authorization</c> scheme SMTP2GO adds to callbacks. Omit for none.</summary>
    [JsonPropertyName("auth_header_type")]
    public WebhookAuthHeaderType? AuthHeaderType { get; init; }

    /// <summary>The <c>Authorization</c> value: <c>base64(user:pass)</c> for Basic, a token for Bearer. Required when <see cref="AuthHeaderType"/> is set.</summary>
    [JsonPropertyName("auth_header_value")]
    public string? AuthHeaderValue { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        WebhookRequestValidator.Validate(Url, Events, SmsEvents, OutputFormat, AuthHeaderType, AuthHeaderValue, urlRequired: true, errors);
    }
}
