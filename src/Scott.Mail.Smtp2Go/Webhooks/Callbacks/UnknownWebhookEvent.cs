namespace Scott.Mail.Smtp2Go.Webhooks;

/// <summary>A callback whose <c>event</c> this library does not know. <c>id</c> and <c>time</c> are mapped; every other field is in <see cref="WebhookEvent.Extra"/>.</summary>
public sealed record UnknownWebhookEvent : WebhookEvent
{
}
