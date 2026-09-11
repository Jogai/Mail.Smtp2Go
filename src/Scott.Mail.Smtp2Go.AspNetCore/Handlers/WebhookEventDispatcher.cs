using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Scott.Mail.Smtp2Go.Webhooks;

namespace Scott.Mail.Smtp2Go.AspNetCore;

/// <summary>
/// The default <see cref="IWebhookEventDispatcher"/>. The callback hierarchy is closed (the core parser only produces the library's types), so the walk from the
/// concrete type to <see cref="WebhookEvent"/> is a fixed table of generic instantiations rather than reflection over <c>MakeGenericType</c>: trim- and AOT-safe,
/// and handlers are resolved from the scope the dispatcher was created in, so scoped handlers see the request's scope.
/// </summary>
internal sealed class WebhookEventDispatcher : IWebhookEventDispatcher
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public WebhookEventDispatcher(IServiceProvider services, ILoggerFactory? loggerFactory = null)
    {
        _services = services;
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger(Smtp2GoWebhookEventIds.CategoryName);
    }

    /// <inheritdoc />
    public async Task<int> DispatchAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        Argument.ThrowIfNull(webhookEvent);

        // Order matters: a subtype must be listed before its base (click before open).
        int invoked = webhookEvent switch
        {
            EmailClickEvent e => await RunAsync(cancellationToken, Handlers<EmailClickEvent>(e), Handlers<EmailOpenEvent>(e), Handlers<EmailWebhookEvent>(e), Handlers<WebhookEvent>(e)).ConfigureAwait(false),
            EmailOpenEvent e => await RunEmailAsync(e, cancellationToken).ConfigureAwait(false),
            EmailProcessedEvent e => await RunEmailAsync(e, cancellationToken).ConfigureAwait(false),
            EmailDeliveredEvent e => await RunEmailAsync(e, cancellationToken).ConfigureAwait(false),
            EmailBounceEvent e => await RunEmailAsync(e, cancellationToken).ConfigureAwait(false),
            EmailSpamEvent e => await RunEmailAsync(e, cancellationToken).ConfigureAwait(false),
            EmailUnsubscribeEvent e => await RunEmailAsync(e, cancellationToken).ConfigureAwait(false),
            EmailResubscribeEvent e => await RunEmailAsync(e, cancellationToken).ConfigureAwait(false),
            EmailRejectEvent e => await RunEmailAsync(e, cancellationToken).ConfigureAwait(false),
            EmailWebhookEvent e => await RunAsync(cancellationToken, Handlers<EmailWebhookEvent>(e), Handlers<WebhookEvent>(e)).ConfigureAwait(false),
            SmsStatusEvent e => await RunAsync(cancellationToken, Handlers<SmsStatusEvent>(e), Handlers<WebhookEvent>(e)).ConfigureAwait(false),
            UnknownWebhookEvent e => await RunAsync(cancellationToken, Handlers<UnknownWebhookEvent>(e), Handlers<WebhookEvent>(e)).ConfigureAwait(false),
            _ => await RunAsync(cancellationToken, Handlers<WebhookEvent>(webhookEvent)).ConfigureAwait(false),
        };

        if (invoked == 0)
        {
            string kind = webhookEvent.Kind.ToString();
            string eventType = webhookEvent.GetType().Name;
            Smtp2GoWebhookLog.NoHandlerRegistered(_logger, kind, eventType);
        }

        return invoked;
    }

    /// <summary>Concrete email type, then <see cref="EmailWebhookEvent"/>, then <see cref="WebhookEvent"/>.</summary>
    private Task<int> RunEmailAsync<TEvent>(TEvent webhookEvent, CancellationToken cancellationToken)
        where TEvent : EmailWebhookEvent
    {
        return RunAsync(cancellationToken, Handlers<TEvent>(webhookEvent), Handlers<EmailWebhookEvent>(webhookEvent), Handlers<WebhookEvent>(webhookEvent));
    }

    /// <summary>The registered handlers for exactly <typeparamref name="TEvent"/> (the container does not apply variance), in registration order, bound to the event.</summary>
    private IEnumerable<Func<CancellationToken, Task>> Handlers<TEvent>(TEvent webhookEvent)
        where TEvent : WebhookEvent
    {
        foreach (IWebhookEventHandler<TEvent> handler in _services.GetServices<IWebhookEventHandler<TEvent>>())
        {
            yield return cancellationToken => handler.HandleAsync(webhookEvent, cancellationToken);
        }
    }

    private static async Task<int> RunAsync(CancellationToken cancellationToken, params IEnumerable<Func<CancellationToken, Task>>[] groups)
    {
        int invoked = 0;
        foreach (IEnumerable<Func<CancellationToken, Task>> group in groups)
        {
            foreach (Func<CancellationToken, Task> handler in group)
            {
                await handler(cancellationToken).ConfigureAwait(false);
                invoked++;
            }
        }

        return invoked;
    }
}
