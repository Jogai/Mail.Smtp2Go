namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>suppression/*</c> family: the addresses and domains SMTP2GO refuses to deliver to. All three endpoints accept <c>subaccount_id</c> through <see cref="RequestOptions.SubaccountId"/>.</summary>
public interface ISuppressionClient
{
    /// <summary><c>POST /suppression/add</c>: suppresses an address or domain.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<SuppressionAddResult>> AddAsync(SuppressionAddRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /suppression/view</c>: one page of suppressions matching the filters. Use <see cref="ViewAllAsync"/> to walk every page.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<SuppressionViewResult>> ViewAsync(SuppressionViewRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Walks every page of <c>suppression/view</c> by <c>continue_token</c>, one API call per page as the enumeration advances.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    IAsyncEnumerable<Suppression> ViewAllAsync(SuppressionViewRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /suppression/remove</c>: lifts the given suppression types from an address or domain and reports which were removed.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<SuppressionRemoveResult>> RemoveAsync(SuppressionRemoveRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);
}
