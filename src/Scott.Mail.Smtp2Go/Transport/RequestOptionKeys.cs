namespace Scott.Mail.Smtp2Go.Transport;

/// <summary>
/// Keys under which the transport stores per-request data on <see cref="HttpRequestMessage"/> (<c>Options</c> on .NET 8+, <c>Properties</c>
/// on netstandard2.0) so that <see cref="DelegatingHandler"/> pipelines, such as the resilience pipeline of the dependency injection package,
/// can read the endpoint descriptor without parsing the URL.
/// </summary>
public static class RequestOptionKeys
{
    /// <summary>Key of the <see cref="Endpoint"/> descriptor.</summary>
    public const string EndpointKey = "Scott.Mail.Smtp2Go.Endpoint";

    /// <summary>Key of the <see cref="RequestOptions.AllowRetry"/> flag (a <see cref="bool"/>; present only when <see langword="true"/>).</summary>
    public const string AllowRetryKey = "Scott.Mail.Smtp2Go.AllowRetry";

#if NET8_0_OR_GREATER
    internal static HttpRequestOptionsKey<Endpoint> Endpoint { get; } = new(EndpointKey);

    internal static HttpRequestOptionsKey<bool> AllowRetry { get; } = new(AllowRetryKey);
#endif

    /// <summary>Reads the <see cref="Endpoint"/> descriptor the transport attached to <paramref name="request"/>.</summary>
    public static bool TryGetEndpoint(HttpRequestMessage request, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Endpoint? endpoint)
    {
        Argument.ThrowIfNull(request);
#if NET8_0_OR_GREATER
        return request.Options.TryGetValue(Endpoint, out endpoint);
#else
        if (request.Properties.TryGetValue(EndpointKey, out object? value) && value is Endpoint found)
        {
            endpoint = found;
            return true;
        }

        endpoint = null;
        return false;
#endif
    }

    /// <summary>Whether the caller opted this request into retries with <see cref="RequestOptions.AllowRetry"/>.</summary>
    public static bool GetAllowRetry(HttpRequestMessage request)
    {
        Argument.ThrowIfNull(request);
#if NET8_0_OR_GREATER
        return request.Options.TryGetValue(AllowRetry, out bool allow) && allow;
#else
        return request.Properties.TryGetValue(AllowRetryKey, out object? value) && value is true;
#endif
    }

    internal static void SetEndpoint(HttpRequestMessage request, Endpoint endpoint)
    {
#if NET8_0_OR_GREATER
        request.Options.Set(Endpoint, endpoint);
#else
        request.Properties[EndpointKey] = endpoint;
#endif
    }

    internal static void SetAllowRetry(HttpRequestMessage request)
    {
#if NET8_0_OR_GREATER
        request.Options.Set(AllowRetry, true);
#else
        request.Properties[AllowRetryKey] = true;
#endif
    }
}
