namespace Scott.Mail.Smtp2Go.Webhooks;

/// <summary><c>processed</c>: SMTP2GO accepted the email and is delivering it. Carries <see cref="EmailWebhookEvent.Recipients"/> rather than <see cref="EmailWebhookEvent.Recipient"/>.</summary>
public sealed record EmailProcessedEvent : EmailWebhookEvent
{
}
