using Scott.Mail.Smtp2Go.Webhooks;

namespace Scott.Mail.Smtp2Go.AspNetCore;

/// <summary>
/// Routes a parsed callback to the registered <see cref="IWebhookEventHandler{TEvent}"/> services. Registered (scoped) by <c>AddSmtp2GoWebhooks</c> and used by
/// <c>MapSmtp2GoWebhook(pattern)</c>; resolve it yourself to replay stored callbacks through the same handlers.
/// </summary>
public interface IWebhookEventDispatcher
{
    /// <summary>
    /// Invokes the handlers for the callback's concrete type, then for each base type up to <see cref="WebhookEvent"/>, sequentially in registration order.
    /// An <see cref="UnknownWebhookEvent"/> reaches only <c>IWebhookEventHandler&lt;UnknownWebhookEvent&gt;</c> and <c>IWebhookEventHandler&lt;WebhookEvent&gt;</c>.
    /// </summary>
    /// <returns>How many handlers ran.</returns>
    Task<int> DispatchAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken);
}
