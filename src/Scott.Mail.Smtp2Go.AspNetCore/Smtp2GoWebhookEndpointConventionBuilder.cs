using Microsoft.AspNetCore.Builder;

namespace Scott.Mail.Smtp2Go.AspNetCore;

/// <summary>
/// The builder <c>MapSmtp2GoWebhook</c> returns: every <see cref="IEndpointConventionBuilder"/> extension (<c>WithName</c>, <c>RequireHost</c>, <c>AddEndpointFilter</c>, ...)
/// still applies, plus the callback-specific conventions. Call them while the application starts, before the first request.
/// </summary>
public sealed class Smtp2GoWebhookEndpointConventionBuilder : IEndpointConventionBuilder
{
    private readonly IEndpointConventionBuilder _inner;
    private readonly Smtp2GoWebhookEndpoint _endpoint;

    internal Smtp2GoWebhookEndpointConventionBuilder(IEndpointConventionBuilder inner, Smtp2GoWebhookEndpoint endpoint)
    {
        _inner = inner;
        _endpoint = endpoint;
    }

    /// <inheritdoc />
    public void Add(Action<EndpointBuilder> convention)
    {
        _inner.Add(convention);
    }

    /// <inheritdoc />
    public void Finally(Action<EndpointBuilder> finallyConvention)
    {
        _inner.Finally(finallyConvention);
    }

    /// <summary>Adjusts this endpoint's copy of the <see cref="Smtp2GoWebhookOptions"/> (body limit, accepted media types, error status, custom header names) without touching other endpoints.</summary>
    public Smtp2GoWebhookEndpointConventionBuilder WithOptions(Action<Smtp2GoWebhookOptions> configure)
    {
        Argument.ThrowIfNull(configure);
        configure(_endpoint.Options);
        _endpoint.Options.Validate();
        return this;
    }

    /// <summary>
    /// Requires <c>Authorization: Basic</c> with these credentials: the webhook's <c>auth_header_type: basic</c> setting, or credentials in the webhook URL
    /// (<c>https://user:pass@host/path</c>), which SMTP2GO sends as the same header. Compared in constant time. Calling this and <see cref="RequireBearer(string)"/>
    /// accepts either; calling it twice accepts either pair (useful while rotating).
    /// </summary>
    public Smtp2GoWebhookEndpointConventionBuilder RequireBasicAuth(string username, string password)
    {
        Argument.ThrowIfNullOrEmpty(username);
        Argument.ThrowIfNull(password);
        return RequireBasicAuth(_ => (username, password));
    }

    /// <summary>Like <see cref="RequireBasicAuth(string, string)"/>, with the credentials read from the request's services on every request (for example from <c>IOptionsMonitor</c> or a secret store).</summary>
    public Smtp2GoWebhookEndpointConventionBuilder RequireBasicAuth(Func<IServiceProvider, (string Username, string Password)> credentials)
    {
        Argument.ThrowIfNull(credentials);
        _endpoint.AddCredential(new WebhookCredential.Basic(credentials));
        return this;
    }

    /// <summary>Requires <c>Authorization: Bearer</c> with this token: the webhook's <c>auth_header_type: bearer</c> setting. Compared in constant time.</summary>
    public Smtp2GoWebhookEndpointConventionBuilder RequireBearer(string token)
    {
        Argument.ThrowIfNullOrEmpty(token);
        return RequireBearer(_ => token);
    }

    /// <summary>Like <see cref="RequireBearer(string)"/>, with the token read from the request's services on every request.</summary>
    public Smtp2GoWebhookEndpointConventionBuilder RequireBearer(Func<IServiceProvider, string> token)
    {
        Argument.ThrowIfNull(token);
        _endpoint.AddCredential(new WebhookCredential.Bearer(token));
        return this;
    }
}
