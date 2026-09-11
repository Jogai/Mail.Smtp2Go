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

    /// <summary><c>POST /email/batch</c>: sends up to 1,000 emails in one call and returns one item per email, in request order.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<IReadOnlyList<EmailBatchItem>>> SendBatchAsync(EmailBatchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /email/scheduled/search</c>: one page of queued emails. Use <see cref="SearchScheduledAllAsync"/> to walk every page.</summary>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<IReadOnlyList<ScheduledEmail>>> SearchScheduledAsync(ScheduledEmailSearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Walks <c>/email/scheduled/search</c> from <see cref="ScheduledEmailSearchRequest.Page"/> (default 1), incrementing <c>page</c> until a page comes back
    /// empty or shorter than <see cref="ScheduledEmailSearchRequest.Limit"/>. Each page is one API call, made as the enumeration advances.
    /// </summary>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    IAsyncEnumerable<ScheduledEmail> SearchScheduledAllAsync(ScheduledEmailSearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /email/scheduled/remove</c>: cancels a queued email. The response <c>data</c> is undocumented and returned raw.</summary>
    /// <param name="scheduleId">The <c>schedule_id</c> from a send response or a scheduled search.</param>
    /// <param name="options">Per-call overrides.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<System.Text.Json.JsonElement>> RemoveScheduledAsync(string scheduleId, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /email/search</c>, deprecated by SMTP2GO (20 calls per minute). Use the activity search instead; there is no paging helper for this endpoint.</summary>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    [Obsolete(ObsoleteMessages.EmailSearch)]
    Task<ApiResponse<EmailSearchResult>> SearchAsync(EmailSearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);
}
