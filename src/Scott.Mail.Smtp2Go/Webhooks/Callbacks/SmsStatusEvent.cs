namespace Scott.Mail.Smtp2Go.Webhooks;

/// <summary>An SMS callback (<c>sms_sending</c>, <c>sms_submitted</c>, <c>sms_delivered</c>, <c>sms_failed</c>, <c>sms_rejected</c>, <c>sms_opt_out</c>) with the docs' "Webhook Parameters - SMS" fields.</summary>
public sealed record SmsStatusEvent : WebhookEvent
{
    /// <summary><c>destination_number</c>.</summary>
    public string? DestinationNumber { get; init; }

    /// <summary><c>email_subject</c>: the subject when the SMS came from an email.</summary>
    public string? EmailSubject { get; init; }

    /// <summary><c>message_content</c>: the SMS body.</summary>
    public string? MessageContent { get; init; }

    /// <summary><c>message_id</c>: the SMS id.</summary>
    public string? MessageId { get; init; }

    /// <summary><c>received_timestamp</c>: when the event happened (UTC).</summary>
    public DateTimeOffset? ReceivedTimestamp { get; init; }

    /// <summary><c>region</c>: the destination region from the number's country code.</summary>
    public string? Region { get; init; }

    /// <summary><c>retry_count</c>.</summary>
    public int? RetryCount { get; init; }

    /// <summary><c>sender_email</c>: the From address.</summary>
    public string? SenderEmail { get; init; }

    /// <summary><c>source_number</c>: the pool or number the SMS was sent from.</summary>
    public string? SourceNumber { get; init; }

    /// <summary><c>status_code</c>.</summary>
    public string? StatusCode { get; init; }

    /// <summary><c>submitted_timestamp</c>: when the SMS reached the downstream provider (UTC).</summary>
    public DateTimeOffset? SubmittedTimestamp { get; init; }
}
