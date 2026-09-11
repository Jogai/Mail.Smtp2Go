using System.Text.Json;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The <c>single_sender_emails/*</c> family: individual addresses verified for sending without a sender domain (or for a subaccount through
/// <see cref="RequestOptions.SubaccountId"/>). Adding an address sends it a verification email.
/// </summary>
public interface ISingleSenderClient
{
    /// <summary><c>POST /single_sender_emails/view</c>: the single sender addresses and whether each is verified, optionally filtered by address.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<SingleSenderViewResult>> ViewAsync(SingleSenderViewRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /single_sender_emails/add</c>: adds an address and emails it a verification link (or resends the link when it is already pending). The docs show only a placeholder for the response, so <c>data</c> is returned raw.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<JsonElement>> AddAsync(SingleSenderAddRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /single_sender_emails/remove</c>: removes an address. The docs describe <c>data</c> as a string (<c>OK</c>).</summary>
    /// <param name="emailAddress">The address to remove.</param>
    /// <param name="options">Per-call overrides.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<string>> RemoveAsync(string emailAddress, RequestOptions? options = null, CancellationToken cancellationToken = default);
}
