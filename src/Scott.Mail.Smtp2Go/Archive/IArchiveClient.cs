namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>archive/*</c> family: search archived emails and fetch the originals. Needs Email Archiving enabled on the account; sent mail appears after roughly two minutes.</summary>
public interface IArchiveClient
{
    /// <summary><c>POST /archive/search</c>: up to <see cref="ArchiveSearchRequest.Limit"/> archived emails matching the filters.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<ArchiveSearchResult>> SearchAsync(ArchiveSearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Walks <c>archive/search</c> by <c>continue_token</c> for as long as the server returns one, one API call per page as the enumeration advances.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    IAsyncEnumerable<ArchivedEmail> SearchAllAsync(ArchiveSearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /archive/email</c>: one archived email by id.</summary>
    /// <param name="emailId">The <c>email_id</c> from a send response, a callback or an activity event.</param>
    /// <param name="options">Per-call overrides.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<ArchivedEmail>> GetAsync(string emailId, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the original message (RFC 5322, suitable for an <c>.eml</c> file) from <see cref="ArchivedEmail.Url"/> into <paramref name="destination"/> through the same
    /// <see cref="HttpClient"/>. The URL is fetched without the API key first; if the server answers 401 or 403 the request is repeated with the key.
    /// </summary>
    /// <param name="email">The archived email; <see cref="ArchivedEmail.Url"/> must be an absolute URL.</param>
    /// <param name="destination">Receives the bytes.</param>
    /// <param name="options">Per-call overrides (API key, timeout).</param>
    /// <param name="cancellationToken">Cancels the download.</param>
    /// <returns>The number of bytes written.</returns>
    /// <exception cref="ArgumentException"><see cref="ArchivedEmail.Url"/> is missing or not absolute.</exception>
    /// <exception cref="Smtp2GoApiException">The download URL answered with a non-success status.</exception>
    Task<long> DownloadOriginalAsync(ArchivedEmail email, Stream destination, RequestOptions? options = null, CancellationToken cancellationToken = default);
}
