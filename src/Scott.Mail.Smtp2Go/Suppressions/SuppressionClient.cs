using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="ISuppressionClient"/> over the shared connection.</summary>
internal sealed class SuppressionClient(Smtp2GoConnection connection) : ISuppressionClient
{
    private static readonly Endpoint s_add = EndpointTable.Get("suppression/add");
    private static readonly Endpoint s_view = EndpointTable.Get("suppression/view");
    private static readonly Endpoint s_remove = EndpointTable.Get("suppression/remove");

    public Task<ApiResponse<SuppressionAddResult>> AddAsync(SuppressionAddRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<SuppressionAddRequest, SuppressionAddResult>(s_add, request, options, cancellationToken);
    }

    public Task<ApiResponse<SuppressionViewResult>> ViewAsync(SuppressionViewRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<SuppressionViewRequest, SuppressionViewResult>(s_view, request, options, cancellationToken);
    }

    public IAsyncEnumerable<Suppression> ViewAllAsync(SuppressionViewRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return ContinueTokenPager.EnumerateAsync(
            request.ContinueToken,
            async (token, ct) =>
            {
                ApiResponse<SuppressionViewResult> response = await ViewAsync(request with { ContinueToken = token }, options, ct).ConfigureAwait(false);
                return new ContinueTokenPager.Page<Suppression>(response.Data?.Results, response.Data?.ContinueToken);
            },
            cancellationToken);
    }

    public Task<ApiResponse<SuppressionRemoveResult>> RemoveAsync(SuppressionRemoveRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<SuppressionRemoveRequest, SuppressionRemoveResult>(s_remove, request, options, cancellationToken);
    }
}
