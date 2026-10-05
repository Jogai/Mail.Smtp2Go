using Scott.Mail.Smtp2Go.Generation;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="IDomainClient"/> over the shared connection.</summary>
[GeneratedClient]
internal sealed partial class DomainClient(Smtp2GoConnection connection) : IDomainClient
{
    private static readonly Endpoint s_remove = EndpointTable.Get("domain/remove");

    public Task<ApiResponse<DomainViewResult>> RemoveAsync(string domain, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNullOrWhiteSpace(domain);
        return connection.SendAsync<DomainRemoveRequest, DomainViewResult>(s_remove, new DomainRemoveRequest { Domain = domain }, options, cancellationToken);
    }
}
