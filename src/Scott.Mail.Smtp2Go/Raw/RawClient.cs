using System.Text.Json;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="IRawClient"/>: resolves the endpoint descriptor and delegates to the connection.</summary>
internal sealed class RawClient(Smtp2GoConnection connection) : IRawClient
{
    public Task<ApiResponse<TResponse>> SendAsync<TRequest, TResponse>(string path, TRequest? body, HttpMethod? method = null, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        return connection.SendAsync<TRequest, TResponse>(Resolve(path, method), body, options, cancellationToken);
    }

    public Task<JsonDocument> SendJsonAsync(string path, JsonElement body, HttpMethod? method = null, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        return connection.SendRawAsync(Resolve(path, method), body, options, cancellationToken);
    }

    /// <summary>The registered descriptor for the path (and method, when given); an unregistered method gets the path's default flags with that method.</summary>
    private static Endpoint Resolve(string path, HttpMethod? method)
    {
        return method is null ? EndpointTable.Get(path) : EndpointTable.Get(path, method);
    }
}
