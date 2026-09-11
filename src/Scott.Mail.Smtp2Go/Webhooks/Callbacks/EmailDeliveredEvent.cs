namespace Scott.Mail.Smtp2Go.Webhooks;

/// <summary><c>delivered</c>: the recipient server accepted the email. <see cref="EmailWebhookEvent.Message"/> holds its 250 response, <see cref="EmailWebhookEvent.Host"/> the server.</summary>
public sealed record EmailDeliveredEvent : EmailWebhookEvent
{
}
