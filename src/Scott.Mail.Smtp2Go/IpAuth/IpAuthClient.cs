using System.Text.Json;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="IIpAuthClient"/> over the shared connection.</summary>
internal sealed class IpAuthClient(Smtp2GoConnection connection) : IIpAuthClient
{
    private static readonly Endpoint s_view = EndpointTable.Get("ip_auth/view");
    private static readonly Endpoint s_patch = EndpointTable.Get("ip_auth/edit", Endpoint.Patch);
    private static readonly Endpoint s_remove = EndpointTable.Get("ip_auth/remove");

    public Task<ApiResponse<AuthenticatedIpViewResult>> ViewAsync(AuthenticatedIpViewRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<AuthenticatedIpViewRequest, AuthenticatedIpViewResult>(s_view, request, options, cancellationToken);
    }

    public Task<ApiResponse<IReadOnlyList<AuthenticatedIp>>> PatchAsync(AuthenticatedIpPatchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<AuthenticatedIpPatchRequest, IReadOnlyList<AuthenticatedIp>>(s_patch, request, options, cancellationToken);
    }

    public Task<ApiResponse<JsonElement>> RemoveAsync(string ipAddress, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNullOrWhiteSpace(ipAddress);
        return connection.SendAsync<AuthenticatedIpRemoveRequest, JsonElement>(s_remove, new AuthenticatedIpRemoveRequest { IpAddress = ipAddress }, options, cancellationToken);
    }
}
