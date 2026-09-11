using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Scott.Mail.Smtp2Go;
using Scott.Mail.Smtp2Go.DependencyInjection;
using Scott.Mail.Smtp2Go.Transport;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the SMTP2GO client: validated <see cref="Smtp2GoOptions"/>, a named <c>HttpClient</c> with the resilience pipeline,
/// <see cref="ISmtp2GoClientFactory"/>, <see cref="ISmtp2GoClient"/> (unnamed registration only, plus a keyed service per name),
/// <see cref="LoggerDiagnostics"/> as <see cref="ISmtp2GoDiagnostics"/> and <see cref="Smtp2GoMetrics"/>.
/// </summary>
public static class Smtp2GoServiceCollectionExtensions
{
    /// <summary>Registers the default client, binding <see cref="Smtp2GoOptions"/> from <paramref name="configuration"/> (typically <c>Configuration.GetSection("Smtp2Go")</c>).</summary>
    public static ISmtp2GoBuilder AddSmtp2Go(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddSmtp2Go(Options.Options.DefaultName, configuration);
    }

    /// <summary>Registers the default client, configuring <see cref="Smtp2GoOptions"/> in code.</summary>
    public static ISmtp2GoBuilder AddSmtp2Go(this IServiceCollection services, Action<Smtp2GoOptions> configure)
    {
        return services.AddSmtp2Go(Options.Options.DefaultName, configure);
    }

    /// <summary>
    /// Registers a named client, binding its <see cref="Smtp2GoOptions"/> from <paramref name="configuration"/>. Resolve it with
    /// <see cref="ISmtp2GoClientFactory.Create"/> or as a keyed <see cref="ISmtp2GoClient"/> service with <paramref name="name"/> as the key.
    /// </summary>
    public static ISmtp2GoBuilder AddSmtp2Go(this IServiceCollection services, string name, IConfiguration configuration)
    {
        Argument.ThrowIfNull(services);
        Argument.ThrowIfNull(name);
        Argument.ThrowIfNull(configuration);

        string path = configuration is IConfigurationSection section ? section.Path : string.Empty;
        return AddCore(services, name, path, builder => builder.Bind(configuration));
    }

    /// <summary>
    /// Registers a named client, configuring its <see cref="Smtp2GoOptions"/> in code. Resolve it with
    /// <see cref="ISmtp2GoClientFactory.Create"/> or as a keyed <see cref="ISmtp2GoClient"/> service with <paramref name="name"/> as the key.
    /// </summary>
    public static ISmtp2GoBuilder AddSmtp2Go(this IServiceCollection services, string name, Action<Smtp2GoOptions> configure)
    {
        Argument.ThrowIfNull(services);
        Argument.ThrowIfNull(name);
        Argument.ThrowIfNull(configure);

        string path = name.Length == 0 ? Smtp2GoConfigurationPaths.DefaultPath : Smtp2GoConfigurationPaths.DefaultPath + ":" + name;
        return AddCore(services, name, path, builder => builder.Configure(configure));
    }

    private static Smtp2GoBuilder AddCore(IServiceCollection services, string name, string configurationPath, Action<OptionsBuilder<Smtp2GoOptions>> bind)
    {
        GetOrAddPaths(services).Set(name, configurationPath);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<Smtp2GoOptions>, Smtp2GoOptionsValidator>(
            provider => new Smtp2GoOptionsValidator(provider.GetRequiredService<Smtp2GoConfigurationPaths>())));

        OptionsBuilder<Smtp2GoOptions> optionsBuilder = services.AddOptions<Smtp2GoOptions>(name);
        bind(optionsBuilder);
        optionsBuilder.ValidateOnStart();

        services.TryAddSingleton<Smtp2GoMetrics>();
        services.TryAddSingleton<ISmtp2GoDiagnostics, LoggerDiagnostics>();
        services.TryAddSingleton<ISmtp2GoClientFactory, Smtp2GoClientFactory>();

        IHttpClientBuilder httpClientBuilder = services.AddHttpClient(Smtp2GoHttpClientNames.For(name), ConfigureHttpClient);
        httpClientBuilder.AddResilienceHandler(Smtp2GoResilienceBuilder.PipelineName, (builder, context) => Smtp2GoResilienceBuilder.Configure(builder, context, name));

        if (name == Options.Options.DefaultName)
        {
            services.TryAddTransient(provider => provider.GetRequiredService<ISmtp2GoClientFactory>().Create(Options.Options.DefaultName));
        }

        services.TryAddKeyedTransient<ISmtp2GoClient>(name, (provider, _) => provider.GetRequiredService<ISmtp2GoClientFactory>().Create(name));

        return new Smtp2GoBuilder(name, services, httpClientBuilder);
    }

    /// <summary>The pipeline owns timeouts; the core transport sets base address and headers per request, so the client itself stays bare.</summary>
    private static void ConfigureHttpClient(HttpClient client)
    {
        client.Timeout = Timeout.InfiniteTimeSpan;
        client.DefaultRequestVersion = HttpVersion.Version20;
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
    }

    /// <summary>One <see cref="Smtp2GoConfigurationPaths"/> instance per service collection, filled at registration time so the validator can name the section.</summary>
    private static Smtp2GoConfigurationPaths GetOrAddPaths(IServiceCollection services)
    {
        foreach (ServiceDescriptor descriptor in services)
        {
            if (descriptor.ServiceType == typeof(Smtp2GoConfigurationPaths) && !descriptor.IsKeyedService && descriptor.ImplementationInstance is Smtp2GoConfigurationPaths existing)
            {
                return existing;
            }
        }

        Smtp2GoConfigurationPaths paths = new();
        services.AddSingleton(paths);
        return paths;
    }
}
