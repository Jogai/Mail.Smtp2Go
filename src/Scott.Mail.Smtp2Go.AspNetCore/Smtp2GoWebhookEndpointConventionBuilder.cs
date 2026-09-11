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
}
