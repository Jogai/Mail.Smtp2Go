namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The <c>allowed_senders/*</c> family: the Allowed or Restricted Senders list of the account (or of a subaccount through
/// <see cref="RequestOptions.SubaccountId"/>) and its <c>mode</c>. Changing the mode to whitelist or blacklist disables the Sender Domains and
/// Single Sender Emails features, so treat <see cref="UpdateAsync"/> as administrative. Every call returns the whole list.
/// </summary>
public interface IAllowedSenderClient
{
    /// <summary><c>POST /allowed_senders/view</c>: the list and its mode.</summary>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<AllowedSendersList>> ViewAsync(RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /allowed_senders/add</c>: adds addresses and domains to the list.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<AllowedSendersList>> AddAsync(AllowedSendersAddRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /allowed_senders/remove</c>: removes addresses and domains from the list; entries that are not on it are ignored.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<AllowedSendersList>> RemoveAsync(AllowedSendersRemoveRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /allowed_senders/update</c>: replaces the whole list and sets its mode. Succeeds even when the Restrict Senders setting is off.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<AllowedSendersList>> UpdateAsync(AllowedSendersUpdateRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);
}
