using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="ISubaccountClient"/> over the shared connection.</summary>
internal sealed class SubaccountClient(Smtp2GoConnection connection) : ISubaccountClient
{
    private static readonly Endpoint s_search = EndpointTable.Get("subaccounts/search");
    private static readonly Endpoint s_add = EndpointTable.Get("subaccount/add");
    private static readonly Endpoint s_edit = EndpointTable.Get("subaccount/edit");
    private static readonly Endpoint s_close = EndpointTable.Get("subaccount/close");
    private static readonly Endpoint s_reopen = EndpointTable.Get("subaccount/reopen");

    public Task<ApiResponse<SubaccountSearchResult>> SearchAsync(SubaccountSearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<SubaccountSearchRequest, SubaccountSearchResult>(s_search, request, options, cancellationToken);
    }

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

    public Task<ApiResponse<Subaccount>> AddAsync(SubaccountAddRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<SubaccountAddRequest, Subaccount>(s_add, request, options, cancellationToken);
    }

    public Task<ApiResponse<Subaccount>> EditAsync(SubaccountEditRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<SubaccountEditRequest, Subaccount>(s_edit, request, options, cancellationToken);
    }

    public Task<ApiResponse<string>> CloseAsync(SubaccountCloseRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<SubaccountCloseRequest, string>(s_close, request, options, cancellationToken);
    }

    public Task<ApiResponse<string>> ReopenAsync(SubaccountReopenRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<SubaccountReopenRequest, string>(s_reopen, request, options, cancellationToken);
    }
}
