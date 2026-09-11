using System.Text.Json;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="IDedicatedIpClient"/> over the shared connection.</summary>
internal sealed class DedicatedIpClient(Smtp2GoConnection connection) : IDedicatedIpClient
{
    private static readonly Endpoint s_view = EndpointTable.Get("dedicated_ips/view");

    public Task<ApiResponse<IReadOnlyList<DedicatedIpPool>>> ViewAsync(RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        return connection.SendAsync<JsonElement, IReadOnlyList<DedicatedIpPool>>(s_view, default, options, cancellationToken);
    }
}
