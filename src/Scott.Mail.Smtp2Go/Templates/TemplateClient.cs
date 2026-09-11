using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="ITemplateClient"/> over the shared connection.</summary>
internal sealed class TemplateClient(Smtp2GoConnection connection) : ITemplateClient
{
    private static readonly Endpoint s_add = EndpointTable.Get("template/add");
    private static readonly Endpoint s_edit = EndpointTable.Get("template/edit");
    private static readonly Endpoint s_delete = EndpointTable.Get("template/delete");
    private static readonly Endpoint s_search = EndpointTable.Get("template/search");
    private static readonly Endpoint s_view = EndpointTable.Get("template/view");

    public Task<ApiResponse<Template>> AddAsync(TemplateAddRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<TemplateAddRequest, Template>(s_add, request, options, cancellationToken);
    }

    public Task<ApiResponse<Template>> UpdateAsync(TemplateUpdateRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<TemplateUpdateRequest, Template>(s_edit, request, options, cancellationToken);
    }

    public Task<ApiResponse<string>> RemoveAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNullOrWhiteSpace(id);
        return connection.SendAsync<TemplateIdRequest, string>(s_delete, new TemplateIdRequest { Id = id }, options, cancellationToken);
    }

    public Task<ApiResponse<TemplateSearchResult>> SearchAsync(TemplateSearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<TemplateSearchRequest, TemplateSearchResult>(s_search, request, options, cancellationToken);
    }

    public IAsyncEnumerable<Template> SearchAllAsync(TemplateSearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return ContinueTokenPager.EnumerateAsync(
            request.ContinueToken,
            async (token, ct) =>
            {
                ApiResponse<TemplateSearchResult> response = await SearchAsync(request with { ContinueToken = token }, options, ct).ConfigureAwait(false);
                return new ContinueTokenPager.Page<Template>(response.Data?.Templates, response.Data?.ContinueToken);
            },
            cancellationToken);
    }

    public Task<ApiResponse<Template>> ViewAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNullOrWhiteSpace(id);
        return connection.SendAsync<TemplateIdRequest, Template>(s_view, new TemplateIdRequest { Id = id }, options, cancellationToken);
    }
}
