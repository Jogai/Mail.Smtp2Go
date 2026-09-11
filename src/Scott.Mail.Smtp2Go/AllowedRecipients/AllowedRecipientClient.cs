using System.Text.Json;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="IAllowedRecipientClient"/> over the shared connection.</summary>
internal sealed class AllowedRecipientClient(Smtp2GoConnection connection) : IAllowedRecipientClient
{
    private static readonly Endpoint s_view = EndpointTable.Get("allowed_recipients/view");
    private static readonly Endpoint s_add = EndpointTable.Get("allowed_recipients/add");
    private static readonly Endpoint s_remove = EndpointTable.Get("allowed_recipients/remove");
    private static readonly Endpoint s_update = EndpointTable.Get("allowed_recipients/update");

    public Task<ApiResponse<AllowedRecipientsList>> ViewAsync(RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        return connection.SendAsync<JsonElement, AllowedRecipientsList>(s_view, default, options, cancellationToken);
    }

    public Task<ApiResponse<AllowedRecipientsList>> AddAsync(AllowedRecipientsAddRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<AllowedRecipientsAddRequest, AllowedRecipientsList>(s_add, request, options, cancellationToken);
    }

    public Task<ApiResponse<AllowedRecipientsList>> RemoveAsync(AllowedRecipientsRemoveRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<AllowedRecipientsRemoveRequest, AllowedRecipientsList>(s_remove, request, options, cancellationToken);
    }

    public Task<ApiResponse<AllowedRecipientsList>> UpdateAsync(AllowedRecipientsUpdateRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<AllowedRecipientsUpdateRequest, AllowedRecipientsList>(s_update, request, options, cancellationToken);
    }
}
