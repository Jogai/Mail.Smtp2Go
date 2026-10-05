using System.Text.Json;
using Scott.Mail.Smtp2Go.Generation;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="IApiKeyClient"/> over the shared connection.</summary>
[GeneratedClient]
internal sealed partial class ApiKeyClient(Smtp2GoConnection connection) : IApiKeyClient
{
    private static readonly Endpoint s_edit = EndpointTable.Get("api_keys/edit", HttpMethod.Post);
    private static readonly Endpoint s_patch = EndpointTable.Get("api_keys/edit", Endpoint.Patch);
    private static readonly Endpoint s_remove = EndpointTable.Get("api_keys/remove");
    private static readonly Endpoint s_permissions = EndpointTable.Get("api_keys/permissions");

    public Task<ApiResponse<IReadOnlyList<ApiKey>>> EditAsync(ApiKeyEditRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<ApiKeyEditRequest, IReadOnlyList<ApiKey>>(s_edit, request, options, cancellationToken);
    }

    public Task<ApiResponse<IReadOnlyList<ApiKey>>> PatchAsync(ApiKeyPatchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<ApiKeyPatchRequest, IReadOnlyList<ApiKey>>(s_patch, request, options, cancellationToken);
    }

    public Task<ApiResponse<JsonElement>> RemoveAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNullOrWhiteSpace(id);
        return connection.SendAsync<ApiKeyRemoveRequest, JsonElement>(s_remove, new ApiKeyRemoveRequest { Id = id }, options, cancellationToken);
    }

    public Task<ApiResponse<IReadOnlyList<string>>> GetPermissionsAsync(RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        return connection.SendAsync<ApiKeyPermissionsRequest, IReadOnlyList<string>>(s_permissions, new ApiKeyPermissionsRequest(), options, cancellationToken);
    }
}
