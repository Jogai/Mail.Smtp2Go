using System.Text.Json;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The <c>api_keys/*</c> family: the API keys of the account (or of a subaccount through <see cref="RequestOptions.SubaccountId"/>).
/// Everything but <see cref="GetPermissionsAsync"/> needs a key with the matching <c>api_keys/*</c> permission; changing a key's <c>endpoints</c>
/// or <c>status</c> can lock the calling key out, so treat these calls as administrative.
/// </summary>
public interface IApiKeyClient
{
    /// <summary><c>POST /api_keys/view</c>: the account's API keys, optionally one key by id or a keyword search. Keys are returned masked.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<IReadOnlyList<ApiKey>>> ViewAsync(ApiKeyViewRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /api_keys/add</c> (5 per minute): creates a key and returns it, unmasked, as a one-element list. The key's <c>endpoints</c> must be a subset of the calling key's.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<IReadOnlyList<ApiKey>>> AddAsync(ApiKeyAddRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /api_keys/edit</c>: replaces a key's settings; fields you omit fall back to their documented defaults. Use <see cref="PatchAsync"/> to change some fields only.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<IReadOnlyList<ApiKey>>> EditAsync(ApiKeyEditRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>PATCH /api_keys/edit</c>: changes the given fields of a key; omitted fields are left as they are.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<IReadOnlyList<ApiKey>>> PatchAsync(ApiKeyPatchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /api_keys/remove</c>: deletes a key. The docs describe no response <c>data</c>, so it is returned raw.</summary>
    /// <param name="id">The full API key to remove.</param>
    /// <param name="options">Per-call overrides.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<JsonElement>> RemoveAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /api_keys/permissions</c>: the endpoints the calling key may use, for example <c>/email/send</c>. Available to every key.</summary>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<IReadOnlyList<string>>> GetPermissionsAsync(RequestOptions? options = null, CancellationToken cancellationToken = default);
}
