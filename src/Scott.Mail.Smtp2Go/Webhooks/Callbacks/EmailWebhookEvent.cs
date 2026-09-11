using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go.Webhooks;

/// <summary>The fields shared by every email callback (the docs' "Webhook Parameters - Email" table plus fields observed live).</summary>
public abstract record EmailWebhookEvent : WebhookEvent
{
    /// <summary><c>email_id</c>: identifies the email across the API (activity search, archive).</summary>
    public string? EmailId { get; init; }

    /// <summary><c>message-id</c> (also seen as <c>Message-Id</c>): the sender's Message-ID header.</summary>
    [JsonPropertyName("message-id")]
    public string? MessageId { get; init; }

    /// <summary><c>subject</c> (also seen as <c>Subject</c>).</summary>
    public string? Subject { get; init; }

    /// <summary><c>sender</c>: the envelope-from address.</summary>
    public string? Sender { get; init; }

    /// <summary><c>from</c>: display name (where available) and address of the From header.</summary>
    public string? From { get; init; }

    /// <summary><c>from_address</c>: the From header address only.</summary>
    public string? FromAddress { get; init; }

    /// <summary><c>from_name</c>: the From header display name; empty when there is none.</summary>
    public string? FromName { get; init; }

    /// <summary><c>rcpt</c>: the recipient this event is about (per-recipient events such as delivered, bounce, open).</summary>
    [JsonPropertyName("rcpt")]
    public string? Recipient { get; init; }

    /// <summary><c>recipients</c>: every recipient of the email (processed events). Split on <c>,</c> and <c>;</c> when it arrives as one string.</summary>
    public IReadOnlyList<string>? Recipients { get; init; }

    /// <summary><c>sendtime</c>: when the email reached SMTP2GO (UTC).</summary>
    [JsonPropertyName("sendtime")]
    public DateTimeOffset? SendTime { get; init; }

    /// <summary><c>auth</c>: the SMTP username, API key or IP address that sent the email.</summary>
    public string? Auth { get; init; }

    /// <summary><c>srchost</c>: the end-user IP for open/click events, or the submitting IP for processed events.</summary>
    [JsonPropertyName("srchost")]
    public string? SourceHost { get; init; }

    /// <summary><c>host</c>: the recipient server (delivered, bounce).</summary>
    public string? Host { get; init; }

    /// <summary><c>message</c>: the recipient server's response or the error message, where available.</summary>
    public string? Message { get; init; }

    /// <summary><c>context</c>: additional information on the event, where available.</summary>
    public string? Context { get; init; }

    /// <summary>The custom headers declared in <see cref="WebhookParserOptions.KnownCustomHeaders"/> that arrived, keyed by the declared name. <see langword="null"/> when none arrived.</summary>
    public IReadOnlyDictionary<string, string>? CustomHeaders { get; init; }
}
