namespace Scott.Mail.Smtp2Go.Webhooks;

/// <summary><c>unsubscribe</c> (docs) or <c>unsubscribed</c> (live): the recipient used the unsubscribe footer; the address is now suppressed.</summary>
public sealed record EmailUnsubscribeEvent : EmailWebhookEvent
{
}
