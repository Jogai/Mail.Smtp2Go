namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>email/*</c> family: sending (JSON, MIME, batch), scheduled-email search and removal.</summary>
public interface IEmailClient
{
    /// <summary>
    /// <c>POST /email/send</c>. The API answers 200 even when recipients fail; inspect <see cref="EmailSendResult.Failed"/> or call
    /// <see cref="EmailSendResultExtensions.EnsureAccepted"/>. A <see langword="null"/> <see cref="EmailSendRequest.FastAccept"/> is filled from
    /// <see cref="Smtp2GoClientOptions.DefaultFastAccept"/>.
    /// </summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<EmailSendResult>> SendAsync(EmailSendRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /email/mime</c>: sends a complete Base64-encoded MIME message. Same response shape and <c>fastaccept</c> defaulting as <see cref="SendAsync"/>.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<EmailSendResult>> SendMimeAsync(EmailMimeRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);
}
