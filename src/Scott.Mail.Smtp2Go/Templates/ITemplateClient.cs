namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>template/*</c> family: the email templates referenced by <see cref="EmailSendRequest.TemplateId"/>. The API paths are <c>template/add</c>, <c>template/edit</c>, <c>template/delete</c>, <c>template/search</c> and <c>template/view</c>.</summary>
public interface ITemplateClient
{
    /// <summary><c>POST /template/add</c>: creates a template and returns it.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<Template>> AddAsync(TemplateAddRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /template/edit</c>: changes the given fields of a template and returns it.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<Template>> UpdateAsync(TemplateUpdateRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /template/delete</c>: deletes a template. The response <c>data</c> is a message such as <c>Successfully deleted template 'example'</c>.</summary>
    /// <param name="id">The case-sensitive template id.</param>
    /// <param name="options">Per-call overrides.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<string>> RemoveAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /template/search</c>: one page of templates without bodies. Use <see cref="SearchAllAsync"/> to walk every page.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<TemplateSearchResult>> SearchAsync(TemplateSearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Walks every page of <c>template/search</c> by <c>continue_token</c>, one API call per page as the enumeration advances.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    IAsyncEnumerable<Template> SearchAllAsync(TemplateSearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /template/view</c>: one template with its bodies and variables.</summary>
    /// <param name="id">The case-sensitive template id.</param>
    /// <param name="options">Per-call overrides.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<Template>> ViewAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default);
}
