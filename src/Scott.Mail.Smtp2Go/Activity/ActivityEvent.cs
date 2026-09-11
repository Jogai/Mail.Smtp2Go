using System.Text.Json;
using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json;

namespace Scott.Mail.Smtp2Go;

/// <summary>One event from <c>activity/search</c>, per the documented event object. <see cref="Event"/> is the typed view of <see cref="EventRaw"/>.</summary>
public sealed record ActivityEvent
{
    /// <summary>The From header address.</summary>
    [JsonPropertyName("from")]
    public string? From { get; init; }

    /// <summary>The recipient this event is about.</summary>
    [JsonPropertyName("recipient")]
    public string? Recipient { get; init; }

    /// <summary>The subaccount name (<c>Master account</c> for the main account).</summary>
    [JsonPropertyName("subaccount_name")]
    public string? SubaccountName { get; init; }

    /// <summary>The id of the email that generated the event.</summary>
    [JsonPropertyName("email_id")]
    public string? EmailId { get; init; }

    /// <summary>When the event happened (UTC).</summary>
    [JsonPropertyName("date")]
    public DateTimeOffset? Date { get; init; }

    /// <summary>The <c>event</c> string exactly as returned (for example <c>soft-bounced</c>).</summary>
    [JsonPropertyName("event")]
    public string? EventRaw { get; init; }

    /// <summary><see cref="EventRaw"/> as an <see cref="ActivityEventType"/>; <see cref="ActivityEventType.Unknown"/> for names this library does not know.</summary>
    [JsonIgnore]
    public ActivityEventType Event => EventRaw is not null && EnumNameTable<ActivityEventType>.TryParse(EventRaw, out ActivityEventType parsed) ? parsed : ActivityEventType.Unknown;

    /// <summary>All recipients of the email.</summary>
    [JsonPropertyName("recipients")]
    public IReadOnlyList<string>? Recipients { get; init; }

    /// <summary>The subject.</summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; init; }

    /// <summary>The SMTP user or API key that sent the email.</summary>
    [JsonPropertyName("username")]
    public string? Username { get; init; }

    /// <summary>The Reply-To header, if present.</summary>
    [JsonPropertyName("reply_to")]
    public string? ReplyTo { get; init; }

    /// <summary>The From header address.</summary>
    [JsonPropertyName("sender")]
    public string? Sender { get; init; }

    /// <summary>The From header including the display name, if present.</summary>
    [JsonPropertyName("sender_full")]
    public string? SenderFull { get; init; }

    /// <summary>The To header.</summary>
    [JsonPropertyName("to")]
    public string? To { get; init; }

    /// <summary>The CC header.</summary>
    [JsonPropertyName("cc")]
    public string? Cc { get; init; }

    /// <summary>The BCC header.</summary>
    [JsonPropertyName("bcc")]
    public string? Bcc { get; init; }

    /// <summary>The receiving server's SMTP response.</summary>
    [JsonPropertyName("smtp_response")]
    public string? SmtpResponse { get; init; }

    /// <summary>Why the event occurred, if present.</summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>The IP address of the host associated with the event.</summary>
    [JsonPropertyName("host")]
    public string? Host { get; init; }

    /// <summary>The originating IP address (processed events).</summary>
    [JsonPropertyName("originating_host")]
    public string? OriginatingHost { get; init; }

    /// <summary>The error message, on events that carry one.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>Email client information (open and click events); shape undocumented, kept raw.</summary>
    [JsonPropertyName("email_client")]
    public JsonElement? EmailClient { get; init; }

    /// <summary>Additional metadata for open and click events; shape undocumented, kept raw.</summary>
    [JsonPropertyName("metadata")]
    public JsonElement? Metadata { get; init; }

    /// <summary>The outbound IP address, if available.</summary>
    [JsonPropertyName("outbound_ip")]
    public string? OutboundIp { get; init; }

    /// <summary>The email size in bytes.</summary>
    [JsonPropertyName("byte_size")]
    public long? ByteSize { get; init; }

    /// <summary>The full headers, when <see cref="ActivitySearchRequest.IncludeHeaders"/> was set.</summary>
    [JsonPropertyName("headers")]
    public string? Headers { get; init; }

    /// <summary>The headers requested through <see cref="ActivitySearchRequest.CustomHeaders"/>.</summary>
    [JsonPropertyName("custom_headers")]
    public IReadOnlyDictionary<string, string>? CustomHeaders { get; init; }

    /// <summary>Delivery attempts so far (processed events).</summary>
    [JsonPropertyName("delivery_attempts")]
    public IReadOnlyList<DeliveryAttempt>? DeliveryAttempts { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
