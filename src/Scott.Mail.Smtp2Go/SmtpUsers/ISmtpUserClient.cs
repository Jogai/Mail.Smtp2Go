namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The <c>users/smtp/*</c> family: the SMTP users of the account (or of a subaccount through <see cref="RequestOptions.SubaccountId"/>).
/// <c>view</c>, <c>add</c>, <c>edit</c> and <c>remove</c> return the affected users, including their passwords; treat the responses as secrets.
/// </summary>
public interface ISmtpUserClient
{
    /// <summary><c>POST /users/smtp/view</c>: every SMTP user, or one by username, with the account's default rate limit.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<SmtpUserViewResult>> ViewAsync(SmtpUserViewRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /users/smtp/add</c>: creates a user and returns it as a one-element list. Leave <see cref="SmtpUserAddRequest.EmailPassword"/> empty for a generated password, which the response carries.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<IReadOnlyList<SmtpUser>>> AddAsync(SmtpUserAddRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /users/smtp/edit</c>: replaces a user's settings; fields you omit fall back to their documented defaults. Use <see cref="PatchAsync"/> to change some fields only.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<IReadOnlyList<SmtpUser>>> EditAsync(SmtpUserEditRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>PATCH /users/smtp/edit</c>: changes the given fields of a user; omitted fields are left as they are.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<IReadOnlyList<SmtpUser>>> PatchAsync(SmtpUserPatchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /users/smtp/remove</c>: deletes a user and returns it as a one-element list.</summary>
    /// <param name="username">The username to remove.</param>
    /// <param name="options">Per-call overrides.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<IReadOnlyList<SmtpUser>>> RemoveAsync(string username, RequestOptions? options = null, CancellationToken cancellationToken = default);
}
