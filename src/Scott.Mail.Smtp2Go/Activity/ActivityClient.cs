using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="IActivityClient"/> over the shared connection.</summary>
internal sealed class ActivityClient(Smtp2GoConnection connection) : IActivityClient
{
    private static readonly Endpoint s_search = EndpointTable.Get("activity/search");

    public async Task<ApiResponse<ActivitySearchResult>> SearchAsync(ActivitySearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        if (connection.GetThrottle(s_search.RateLimit) is { } throttle)
        {
            await throttle.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        return await connection.SendAsync<ActivitySearchRequest, ActivitySearchResult>(s_search, request, options, cancellationToken).ConfigureAwait(false);
    }

    public IAsyncEnumerable<ActivityEvent> SearchAllAsync(ActivitySearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return ContinueTokenPager.EnumerateAsync(
            request.ContinueToken,
            async (token, ct) =>
            {
                ApiResponse<ActivitySearchResult> response = await SearchAsync(request with { ContinueToken = token }, options, ct).ConfigureAwait(false);
                return new ContinueTokenPager.Page<ActivityEvent>(response.Data?.Events, response.Data?.ContinueToken);
            },
            cancellationToken);
    }
}
