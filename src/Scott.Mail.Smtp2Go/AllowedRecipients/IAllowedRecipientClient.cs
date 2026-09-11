namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The <c>allowed_recipients/*</c> family: the Allowed Recipients list of the account (or of a subaccount through
/// <see cref="RequestOptions.SubaccountId"/>) and whether it is applied when sending. Every call returns the whole list.
/// </summary>
public interface IAllowedRecipientClient
{
    /// <summary><c>POST /allowed_recipients/view</c>: the list and whether it is enabled.</summary>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<AllowedRecipientsList>> ViewAsync(RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /allowed_recipients/add</c>: adds addresses and domains to the list, optionally switching it on or off.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<AllowedRecipientsList>> AddAsync(AllowedRecipientsAddRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /allowed_recipients/remove</c>: removes addresses and domains from the list; entries that are not on it are ignored.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<AllowedRecipientsList>> RemoveAsync(AllowedRecipientsRemoveRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /allowed_recipients/update</c>: replaces the whole list and sets whether it is applied. Succeeds even when the setting is not in use.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<AllowedRecipientsList>> UpdateAsync(AllowedRecipientsUpdateRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);
}
