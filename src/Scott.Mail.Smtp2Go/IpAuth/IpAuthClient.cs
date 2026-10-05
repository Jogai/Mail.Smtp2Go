using System.Text.Json;
using Scott.Mail.Smtp2Go.Generation;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="IIpAuthClient"/> over the shared connection.</summary>
[GeneratedClient]
internal sealed partial class IpAuthClient(Smtp2GoConnection connection) : IIpAuthClient
{
    private static readonly Endpoint s_remove = EndpointTable.Get("ip_auth/remove");

    public Task<ApiResponse<JsonElement>> RemoveAsync(string ipAddress, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNullOrWhiteSpace(ipAddress);
        return connection.SendAsync<AuthenticatedIpRemoveRequest, JsonElement>(s_remove, new AuthenticatedIpRemoveRequest { IpAddress = ipAddress }, options, cancellationToken);
    }
}
