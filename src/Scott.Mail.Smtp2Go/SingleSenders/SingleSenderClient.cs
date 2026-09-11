using System.Text.Json;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="ISingleSenderClient"/> over the shared connection.</summary>
internal sealed class SingleSenderClient(Smtp2GoConnection connection) : ISingleSenderClient
{
    private static readonly Endpoint s_view = EndpointTable.Get("single_sender_emails/view");
    private static readonly Endpoint s_add = EndpointTable.Get("single_sender_emails/add");
    private static readonly Endpoint s_remove = EndpointTable.Get("single_sender_emails/remove");

    public Task<ApiResponse<SingleSenderViewResult>> ViewAsync(SingleSenderViewRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<SingleSenderViewRequest, SingleSenderViewResult>(s_view, request, options, cancellationToken);
    }

    public Task<ApiResponse<JsonElement>> AddAsync(SingleSenderAddRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<SingleSenderAddRequest, JsonElement>(s_add, request, options, cancellationToken);
    }

    public Task<ApiResponse<string>> RemoveAsync(string emailAddress, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNullOrWhiteSpace(emailAddress);
        return connection.SendAsync<SingleSenderRemoveRequest, string>(s_remove, new SingleSenderRemoveRequest { EmailAddress = emailAddress }, options, cancellationToken);
    }
}
