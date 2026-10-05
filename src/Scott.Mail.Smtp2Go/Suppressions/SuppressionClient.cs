using Scott.Mail.Smtp2Go.Generation;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="ISuppressionClient"/> over the shared connection.</summary>
[GeneratedClient]
internal sealed partial class SuppressionClient(Smtp2GoConnection connection) : ISuppressionClient
{
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
}
