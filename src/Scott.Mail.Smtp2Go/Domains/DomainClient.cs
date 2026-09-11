using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="IDomainClient"/> over the shared connection.</summary>
internal sealed class DomainClient(Smtp2GoConnection connection) : IDomainClient
{
    private static readonly Endpoint s_view = EndpointTable.Get("domain/view");
    private static readonly Endpoint s_add = EndpointTable.Get("domain/add");
    private static readonly Endpoint s_verify = EndpointTable.Get("domain/verify");
    private static readonly Endpoint s_remove = EndpointTable.Get("domain/remove");
    private static readonly Endpoint s_tracking = EndpointTable.Get("domain/tracking");
    private static readonly Endpoint s_returnPath = EndpointTable.Get("domain/returnpath");
    private static readonly Endpoint s_subaccountAccess = EndpointTable.Get("domain/subaccount_access");

    public Task<ApiResponse<DomainViewResult>> ViewAsync(DomainViewRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<DomainViewRequest, DomainViewResult>(s_view, request, options, cancellationToken);
    }

    public Task<ApiResponse<DomainViewResult>> AddAsync(DomainAddRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<DomainAddRequest, DomainViewResult>(s_add, request, options, cancellationToken);
    }

    public Task<ApiResponse<DomainViewResult>> VerifyAsync(DomainVerifyRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<DomainVerifyRequest, DomainViewResult>(s_verify, request, options, cancellationToken);
    }

    public Task<ApiResponse<DomainViewResult>> RemoveAsync(string domain, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNullOrWhiteSpace(domain);
        return connection.SendAsync<DomainRemoveRequest, DomainViewResult>(s_remove, new DomainRemoveRequest { Domain = domain }, options, cancellationToken);
    }

    public Task<ApiResponse<DomainViewResult>> SetTrackingSubdomainAsync(DomainTrackingRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<DomainTrackingRequest, DomainViewResult>(s_tracking, request, options, cancellationToken);
    }

    public Task<ApiResponse<DomainViewResult>> SetReturnPathSubdomainAsync(DomainReturnPathRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<DomainReturnPathRequest, DomainViewResult>(s_returnPath, request, options, cancellationToken);
    }

    public Task<ApiResponse<DomainSubaccountAccessResult>> SetSubaccountAccessAsync(DomainSubaccountAccessRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<DomainSubaccountAccessRequest, DomainSubaccountAccessResult>(s_subaccountAccess, request, options, cancellationToken);
    }
}
