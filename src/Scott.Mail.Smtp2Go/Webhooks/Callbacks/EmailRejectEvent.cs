namespace Scott.Mail.Smtp2Go.Webhooks;

/// <summary><c>reject</c> or <c>rejected</c>: SMTP2GO refused to send (suppressed recipient, unverified sender, sandboxed credentials). <see cref="EmailWebhookEvent.Message"/> and <see cref="EmailWebhookEvent.Context"/> say why.</summary>
public sealed record EmailRejectEvent : EmailWebhookEvent
{
}
