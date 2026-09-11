using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Scott.Mail.Smtp2Go.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers what <c>MapSmtp2GoWebhook(pattern)</c> needs: <see cref="Smtp2GoWebhookOptions"/>, the scoped <see cref="IWebhookEventDispatcher"/> and a singleton
/// <see cref="ISmtp2GoSourceIpResolver"/> (<see cref="Smtp2GoSourceIpResolver"/> on the container's <see cref="TimeProvider"/>, if any). Handlers are not scanned:
/// register each <see cref="IWebhookEventHandler{TEvent}"/> yourself with the lifetime it needs.
/// </summary>
public static class Smtp2GoWebhookServiceCollectionExtensions
{
    /// <summary>Registers the dispatcher, the source-IP resolver and the options with their defaults.</summary>
    public static IServiceCollection AddSmtp2GoWebhooks(this IServiceCollection services)
    {
        return services.AddSmtp2GoWebhooks(static _ => { });
    }

    /// <summary>Registers the dispatcher, the source-IP resolver and the options, configured by <paramref name="configure"/> (applied to every mapped endpoint; <c>WithOptions</c> adjusts one).</summary>
    public static IServiceCollection AddSmtp2GoWebhooks(this IServiceCollection services, Action<Smtp2GoWebhookOptions> configure)
    {
        Argument.ThrowIfNull(services);
        Argument.ThrowIfNull(configure);

        services.AddOptions<Smtp2GoWebhookOptions>().Configure(configure);
        services.TryAddScoped<IWebhookEventDispatcher, WebhookEventDispatcher>();
        services.TryAddSingleton<ISmtp2GoSourceIpResolver>(static provider => new Smtp2GoSourceIpResolver(provider.GetService<TimeProvider>(), provider.GetService<ILoggerFactory>()));
        return services;
    }
}
