using System.Text.Json;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="IAllowedSenderClient"/> over the shared connection.</summary>
internal sealed class AllowedSenderClient(Smtp2GoConnection connection) : IAllowedSenderClient
{
    private static readonly Endpoint s_view = EndpointTable.Get("allowed_senders/view");
    private static readonly Endpoint s_add = EndpointTable.Get("allowed_senders/add");
    private static readonly Endpoint s_remove = EndpointTable.Get("allowed_senders/remove");
    private static readonly Endpoint s_update = EndpointTable.Get("allowed_senders/update");

    public Task<ApiResponse<AllowedSendersList>> ViewAsync(RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        return connection.SendAsync<JsonElement, AllowedSendersList>(s_view, default, options, cancellationToken);
    }

    public Task<ApiResponse<AllowedSendersList>> AddAsync(AllowedSendersAddRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<AllowedSendersAddRequest, AllowedSendersList>(s_add, request, options, cancellationToken);
    }

    public Task<ApiResponse<AllowedSendersList>> RemoveAsync(AllowedSendersRemoveRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<AllowedSendersRemoveRequest, AllowedSendersList>(s_remove, request, options, cancellationToken);
    }

    public Task<ApiResponse<AllowedSendersList>> UpdateAsync(AllowedSendersUpdateRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<AllowedSendersUpdateRequest, AllowedSendersList>(s_update, request, options, cancellationToken);
    }
}
