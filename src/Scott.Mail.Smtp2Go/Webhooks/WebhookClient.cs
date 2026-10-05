using Scott.Mail.Smtp2Go.Generation;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="IWebhookClient"/> over the shared connection.</summary>
[GeneratedClient]
internal sealed partial class WebhookClient(Smtp2GoConnection connection) : IWebhookClient
{
    private static readonly Endpoint s_remove = EndpointTable.Get("webhook/remove");

    public Task<ApiResponse<Webhook>> RemoveAsync(long id, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        return connection.SendAsync<WebhookRemoveRequest, Webhook>(s_remove, new WebhookRemoveRequest { Id = id }, options, cancellationToken);
    }

    public async Task<ApiResponse<Webhook>> AddOrUpdateByUrlAsync(WebhookAddRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        ApiResponse<IReadOnlyList<Webhook>> existing = await ViewAsync(options, cancellationToken).ConfigureAwait(false);
        Webhook? match = existing.Data?.FirstOrDefault(w => w.Id is not null && string.Equals(w.Url, request.Url, StringComparison.Ordinal));
        return match is null
            ? await AddAsync(request, options, cancellationToken).ConfigureAwait(false)
            : await EditAsync(WebhookEditRequest.From(match.Id!.Value, request), options, cancellationToken).ConfigureAwait(false);
    }
}
