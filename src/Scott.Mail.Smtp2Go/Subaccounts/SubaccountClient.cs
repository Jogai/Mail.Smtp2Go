using Scott.Mail.Smtp2Go.Generation;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="ISubaccountClient"/> over the shared connection.</summary>
[GeneratedClient]
internal sealed partial class SubaccountClient(Smtp2GoConnection connection) : ISubaccountClient
{
    public IAsyncEnumerable<Subaccount> SearchAllAsync(SubaccountSearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return ContinueTokenPager.EnumerateAsync(
            request.ContinueToken,
            async (token, ct) =>
            {
                ApiResponse<SubaccountSearchResult> response = await SearchAsync(request with { ContinueToken = token }, options, ct).ConfigureAwait(false);
                return new ContinueTokenPager.Page<Subaccount>(response.Data?.Subaccounts, response.Data?.ContinueToken);
            },
            cancellationToken);
    }
}
