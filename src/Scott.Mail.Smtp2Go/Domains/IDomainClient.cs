namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The <c>domain/*</c> family: the sender domains of the account (or of a subaccount through <see cref="RequestOptions.SubaccountId"/>), their DKIM
/// and return-path verification, tracking subdomains and subaccount access. Every call but <see cref="SetSubaccountAccessAsync"/> returns the
/// affected domains in the same <see cref="DomainViewResult"/> shape.
/// </summary>
public interface IDomainClient
{
    /// <summary><c>POST /domain/view</c>: every sender domain, or one by name. <c>setup_link</c> is only filled when a single domain is returned.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<DomainViewResult>> ViewAsync(DomainViewRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /domain/add</c>: adds a sender domain you own and (by default) verifies it straight away.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<DomainViewResult>> AddAsync(DomainAddRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /domain/verify</c>: verifies a domain now instead of waiting for the periodic check (every 7 minutes).</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<DomainViewResult>> VerifyAsync(DomainVerifyRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /domain/remove</c>: deletes a sender domain and returns the remaining ones.</summary>
    /// <param name="domain">The domain to delete.</param>
    /// <param name="options">Per-call overrides.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<DomainViewResult>> RemoveAsync(string domain, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /domain/tracking</c>: renames the tracking subdomain (opens, clicks, unsubscribe links) of a sender domain.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<DomainViewResult>> SetTrackingSubdomainAsync(DomainTrackingRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /domain/returnpath</c>: renames the return-path subdomain (where bounces go) of a sender domain.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<DomainViewResult>> SetReturnPathSubdomainAsync(DomainReturnPathRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /domain/subaccount_access</c>: lets the given subaccounts send from a verified domain of the master account. Master-account keys only; the endpoint does not take <c>subaccount_id</c>.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<DomainSubaccountAccessResult>> SetSubaccountAccessAsync(DomainSubaccountAccessRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);
}
