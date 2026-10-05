using Scott.Mail.Smtp2Go.Generation;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="ISmtpUserClient"/> over the shared connection.</summary>
[GeneratedClient]
internal sealed partial class SmtpUserClient(Smtp2GoConnection connection) : ISmtpUserClient
{
    private static readonly Endpoint s_edit = EndpointTable.Get("users/smtp/edit", HttpMethod.Post);
    private static readonly Endpoint s_patch = EndpointTable.Get("users/smtp/edit", Endpoint.Patch);
    private static readonly Endpoint s_remove = EndpointTable.Get("users/smtp/remove");

    public Task<ApiResponse<IReadOnlyList<SmtpUser>>> EditAsync(SmtpUserEditRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<SmtpUserEditRequest, IReadOnlyList<SmtpUser>>(s_edit, request, options, cancellationToken);
    }

    public Task<ApiResponse<IReadOnlyList<SmtpUser>>> PatchAsync(SmtpUserPatchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<SmtpUserPatchRequest, IReadOnlyList<SmtpUser>>(s_patch, request, options, cancellationToken);
    }

    public Task<ApiResponse<IReadOnlyList<SmtpUser>>> RemoveAsync(string username, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNullOrWhiteSpace(username);
        return connection.SendAsync<SmtpUserRemoveRequest, IReadOnlyList<SmtpUser>>(s_remove, new SmtpUserRemoveRequest { Username = username }, options, cancellationToken);
    }
}
