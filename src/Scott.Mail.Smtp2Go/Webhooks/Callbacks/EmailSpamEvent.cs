namespace Scott.Mail.Smtp2Go.Webhooks;

/// <summary><c>spam</c> (docs) or <c>spam_complaint</c> (live): the recipient reported the email as spam; the address is now suppressed.</summary>
public sealed record EmailSpamEvent : EmailWebhookEvent
{
}
