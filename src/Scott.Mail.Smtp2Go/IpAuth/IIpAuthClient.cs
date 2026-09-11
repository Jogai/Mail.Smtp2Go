using System.Text.Json;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The <c>ip_auth/*</c> family: the authenticated IPs that may send through the account without credentials (or through a subaccount via
/// <see cref="RequestOptions.SubaccountId"/>). The docs list no <c>add</c> endpoint; entries are created in the SMTP2GO app.
/// </summary>
public interface IIpAuthClient
{
    /// <summary><c>POST /ip_auth/view</c>: every authenticated IP, or one by address, with the account's default rate limit.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<AuthenticatedIpViewResult>> ViewAsync(AuthenticatedIpViewRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>PATCH /ip_auth/edit</c>: changes the given fields of an entry; omitted fields are left as they are. Returns the entry as a one-element list.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<IReadOnlyList<AuthenticatedIp>>> PatchAsync(AuthenticatedIpPatchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /ip_auth/remove</c>: deletes an entry. The docs describe no response <c>data</c>, so it is returned raw.</summary>
    /// <param name="ipAddress">The address of the entry to remove.</param>
    /// <param name="options">Per-call overrides.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<JsonElement>> RemoveAsync(string ipAddress, RequestOptions? options = null, CancellationToken cancellationToken = default);
}
