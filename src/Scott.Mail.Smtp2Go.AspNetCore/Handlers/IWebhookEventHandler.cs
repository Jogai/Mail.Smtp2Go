using System.Diagnostics.CodeAnalysis;
using Scott.Mail.Smtp2Go.Webhooks;

namespace Scott.Mail.Smtp2Go.AspNetCore;

/// <summary>
/// Handles callbacks of <typeparamref name="TEvent"/> and its subtypes. Register implementations explicitly, for example
/// <c>services.AddScoped&lt;IWebhookEventHandler&lt;EmailBounceEvent&gt;, BounceHandler&gt;()</c>; <c>MapSmtp2GoWebhook(pattern)</c> then dispatches every callback
/// to the handlers of its concrete type, then of each base type up to <see cref="WebhookEvent"/> (so <c>IWebhookEventHandler&lt;WebhookEvent&gt;</c> sees everything),
/// sequentially in registration order. Return quickly: SMTP2GO waits 10 seconds; enqueue slow work.
/// </summary>
/// <typeparam name="TEvent">The callback type: a concrete type such as <c>EmailBounceEvent</c>, or a base such as <c>EmailWebhookEvent</c> or <see cref="WebhookEvent"/>.</typeparam>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "It handles WebhookEvent instances; the name is the documented contract, not a .NET event handler delegate.")]
public interface IWebhookEventHandler<in TEvent>
    where TEvent : WebhookEvent
{
    /// <summary>Handles one callback. An exception stops the remaining handlers and makes the endpoint return <see cref="Smtp2GoWebhookOptions.ReturnStatusOnHandlerError"/>.</summary>
    Task HandleAsync(TEvent webhookEvent, CancellationToken cancellationToken);
}
