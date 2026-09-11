namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>activity/search</c> endpoint: every event (processed, delivered, bounced, opened, ...) of every email, with filters. Rate-limited to 60 calls per minute; this client throttles itself accordingly unless <see cref="Smtp2GoClientOptions.ClientSideRateLimiting"/> is off.</summary>
public interface IActivityClient
{
    /// <summary><c>POST /activity/search</c>: one page of up to <see cref="ActivitySearchRequest.Limit"/> events.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<ActivitySearchResult>> SearchAsync(ActivitySearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Walks every page by <c>continue_token</c>, starting from <see cref="ActivitySearchRequest.ContinueToken"/>, one API call per page as the enumeration advances, throttled to the endpoint's 60 calls per minute.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    IAsyncEnumerable<ActivityEvent> SearchAllAsync(ActivitySearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);
}
