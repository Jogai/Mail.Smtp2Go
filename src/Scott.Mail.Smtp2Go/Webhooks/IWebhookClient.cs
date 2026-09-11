namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>webhook/*</c> family: list, add, edit and remove the webhooks that deliver callbacks. Parsing the callbacks themselves is <c>WebhookPayloadParser</c>.</summary>
public interface IWebhookClient
{
    /// <summary><c>POST /webhook/view</c>: every configured webhook. The live API returns an array; a single object (the docs example) is read as a one-item list.</summary>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<IReadOnlyList<Webhook>>> ViewAsync(RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /webhook/add</c>: creates a webhook and returns it, including its <see cref="Webhook.Id"/>. Free plans allow one webhook, paid plans ten.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<Webhook>> AddAsync(WebhookAddRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /webhook/edit</c>: changes the given fields of an existing webhook in place, keeping its id, and returns it.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<Webhook>> EditAsync(WebhookEditRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /webhook/remove</c>: deletes a webhook and returns the removed definition.</summary>
    /// <param name="id">The <see cref="Webhook.Id"/>.</param>
    /// <param name="options">Per-call overrides.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<Webhook>> RemoveAsync(long id, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the webhooks, and edits the one whose <see cref="Webhook.Url"/> equals <see cref="WebhookAddRequest.Url"/> (ordinal comparison) or adds a new one when there is none.
    /// Keeps webhook ids stable across deployments. Two or three API calls.
    /// </summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<Webhook>> AddOrUpdateByUrlAsync(WebhookAddRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);
}
