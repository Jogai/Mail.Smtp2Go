using Microsoft.Extensions.DependencyInjection;

namespace Scott.Mail.Smtp2Go.DependencyInjection;

/// <summary>Returned by <c>AddSmtp2Go</c> so callers can add handlers to the client's <c>HttpClient</c> or register further services.</summary>
public interface ISmtp2GoBuilder
{
    /// <summary>The registration name; <see cref="Microsoft.Extensions.Options.Options.DefaultName"/> for the unnamed client.</summary>
    string Name { get; }

    /// <summary>The service collection the client was added to.</summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// The builder of the named <c>HttpClient</c> (<c>Scott.Mail.Smtp2Go</c> or <c>Scott.Mail.Smtp2Go:{name}</c>); use it to add message handlers,
    /// configure the primary handler or set the handler lifetime. The resilience handler is already attached.
    /// </summary>
    IHttpClientBuilder HttpClientBuilder { get; }
}
