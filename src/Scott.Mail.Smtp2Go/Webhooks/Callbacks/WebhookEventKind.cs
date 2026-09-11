namespace Scott.Mail.Smtp2Go.Webhooks;

/// <summary>The resolved kind of a callback. <see cref="WebhookEvent.EventRaw"/> keeps the exact wire string, which differs between the docs and the live API for several kinds.</summary>
public enum WebhookEventKind
{
    /// <summary>The <c>event</c> value is not one this library knows; the payload is an <see cref="UnknownWebhookEvent"/> with every field in <see cref="WebhookEvent.Extra"/>.</summary>
    Unknown = 0,

    /// <summary><c>processed</c>.</summary>
    EmailProcessed,

    /// <summary><c>delivered</c>.</summary>
    EmailDelivered,

    /// <summary><c>bounce</c>; <see cref="EmailBounceEvent.BounceType"/> says hard or soft.</summary>
    EmailBounce,

    /// <summary><c>open</c> (docs) or <c>opened</c> (live).</summary>
    EmailOpen,

    /// <summary><c>click</c> (docs) or <c>clicked</c> (live).</summary>
    EmailClick,

    /// <summary><c>spam</c> (docs) or <c>spam_complaint</c> (live).</summary>
    EmailSpam,

    /// <summary><c>unsubscribe</c> (docs) or <c>unsubscribed</c> (live).</summary>
    EmailUnsubscribe,

    /// <summary><c>resubscribe</c> or <c>resubscribed</c>.</summary>
    EmailResubscribe,

    /// <summary><c>reject</c> or <c>rejected</c>.</summary>
    EmailReject,

    /// <summary><c>sms_sending</c>.</summary>
    SmsSending,

    /// <summary><c>sms_submitted</c>.</summary>
    SmsSubmitted,

    /// <summary><c>sms_delivered</c>.</summary>
    SmsDelivered,

    /// <summary><c>sms_failed</c>.</summary>
    SmsFailed,

    /// <summary><c>sms_rejected</c>.</summary>
    SmsRejected,

    /// <summary><c>sms_opt_out</c>.</summary>
    SmsOptOut,
}
