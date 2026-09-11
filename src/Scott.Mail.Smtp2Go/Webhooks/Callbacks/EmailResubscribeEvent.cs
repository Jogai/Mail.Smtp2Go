namespace Scott.Mail.Smtp2Go.Webhooks;

/// <summary><c>resubscribe</c> or <c>resubscribed</c>: the recipient resubscribed, or the address was removed from suppressions.</summary>
public sealed record EmailResubscribeEvent : EmailWebhookEvent
{
}
