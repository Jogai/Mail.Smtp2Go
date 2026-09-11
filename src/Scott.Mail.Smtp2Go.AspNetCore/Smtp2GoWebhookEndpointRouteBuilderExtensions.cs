using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Scott.Mail.Smtp2Go.AspNetCore;
using Scott.Mail.Smtp2Go.Webhooks;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Maps a <c>POST</c> endpoint that receives SMTP2GO webhook callbacks: JSON (<c>output_format: json</c>), <c>application/x-www-form-urlencoded</c> (the server
/// default) and <c>multipart/form-data</c> bodies are parsed by the core <c>WebhookPayloadParser</c>; anything else is answered with 415, a body over
/// <see cref="Smtp2GoWebhookOptions.MaxBodyBytes"/> with 413, a malformed body with 400. The endpoint returns 200 with an empty body once the handler completes.
/// </summary>
public static class Smtp2GoWebhookEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps <paramref name="pattern"/> to <paramref name="handler"/>, which receives every parsed callback (switch on the <c>WebhookEvent</c> subtype).
    /// Needs no service registration. When the handler throws, <see cref="Smtp2GoWebhookOptions.ReturnStatusOnHandlerError"/> (default 500) is returned and SMTP2GO retries.
    /// </summary>
    public static Smtp2GoWebhookEndpointConventionBuilder MapSmtp2GoWebhook(this IEndpointRouteBuilder endpoints, string pattern, Func<WebhookEvent, CancellationToken, Task> handler)
    {
        Argument.ThrowIfNull(endpoints);
        Argument.ThrowIfNull(pattern);
        Argument.ThrowIfNull(handler);
        return Map(endpoints, pattern, (_, webhookEvent, cancellationToken) => handler(webhookEvent, cancellationToken), usesHandlerDispatch: false);
    }

    /// <summary>
    /// Maps <paramref name="pattern"/> to the registered <see cref="IWebhookEventHandler{TEvent}"/> services through the <see cref="IWebhookEventDispatcher"/>:
    /// the handlers for the callback's concrete type run first, then those for each base type up to <c>WebhookEvent</c>, in registration order.
    /// Requires <c>services.AddSmtp2GoWebhooks()</c>; throws at mapping time otherwise.
    /// </summary>
    public static Smtp2GoWebhookEndpointConventionBuilder MapSmtp2GoWebhook(this IEndpointRouteBuilder endpoints, string pattern)
    {
        Argument.ThrowIfNull(endpoints);
        Argument.ThrowIfNull(pattern);

        IServiceProviderIsService? isService = endpoints.ServiceProvider.GetService<IServiceProviderIsService>();
        if (isService is not null && !isService.IsService(typeof(IWebhookEventDispatcher)))
        {
            throw new InvalidOperationException($"No {nameof(IWebhookEventDispatcher)} is registered. Call services.AddSmtp2GoWebhooks() before MapSmtp2GoWebhook(\"{pattern}\"), or pass an inline handler.");
        }

        return Map(endpoints, pattern, static (context, webhookEvent, cancellationToken) => context.RequestServices.GetRequiredService<IWebhookEventDispatcher>().DispatchAsync(webhookEvent, cancellationToken), usesHandlerDispatch: true);
    }

    private static Smtp2GoWebhookEndpointConventionBuilder Map(IEndpointRouteBuilder endpoints, string pattern, Func<HttpContext, WebhookEvent, CancellationToken, Task> handler, bool usesHandlerDispatch)
    {
        IServiceProvider services = endpoints.ServiceProvider;
        Smtp2GoWebhookOptions options = (services.GetService<IOptions<Smtp2GoWebhookOptions>>()?.Value ?? new Smtp2GoWebhookOptions()).Clone();
        options.Validate();
        ILoggerFactory loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;

        Smtp2GoWebhookEndpoint endpoint = new(options, handler, loggerFactory);
        IEndpointConventionBuilder inner = endpoints
            .MapPost(pattern, new RequestDelegate(endpoint.InvokeAsync))
            .WithMetadata(new Smtp2GoWebhookMetadata(pattern, usesHandlerDispatch))
            .ExcludeFromDescription();
        return new Smtp2GoWebhookEndpointConventionBuilder(inner, endpoint);
    }
}
