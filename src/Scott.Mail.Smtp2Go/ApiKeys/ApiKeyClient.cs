using System.Text.Json;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="IApiKeyClient"/> over the shared connection.</summary>
internal sealed class ApiKeyClient(Smtp2GoConnection connection) : IApiKeyClient
{
    private static readonly Endpoint s_view = EndpointTable.Get("api_keys/view");
    private static readonly Endpoint s_add = EndpointTable.Get("api_keys/add");
    private static readonly Endpoint s_edit = EndpointTable.Get("api_keys/edit", HttpMethod.Post);
    private static readonly Endpoint s_patch = EndpointTable.Get("api_keys/edit", Endpoint.Patch);
    private static readonly Endpoint s_remove = EndpointTable.Get("api_keys/remove");
    private static readonly Endpoint s_permissions = EndpointTable.Get("api_keys/permissions");

    public Task<ApiResponse<IReadOnlyList<ApiKey>>> ViewAsync(ApiKeyViewRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<ApiKeyViewRequest, IReadOnlyList<ApiKey>>(s_view, request, options, cancellationToken);
    }

    public Task<ApiResponse<IReadOnlyList<ApiKey>>> AddAsync(ApiKeyAddRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<ApiKeyAddRequest, IReadOnlyList<ApiKey>>(s_add, request, options, cancellationToken);
    }

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
