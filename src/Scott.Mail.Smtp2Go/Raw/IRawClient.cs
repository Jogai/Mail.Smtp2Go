using System.Text.Json;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// Calls any SMTP2GO endpoint by path with the same authentication, regional routing, subaccount injection, validation, error mapping and
/// diagnostics as the typed clients. Use it for endpoints that have no typed client yet, or to send undocumented fields.
/// </summary>
public interface IRawClient
{
    /// <summary>
    /// Sends <paramref name="body"/> to <paramref name="path"/> and parses the envelope into <see cref="ApiResponse{TData}"/>.
    /// Both types must be registered in the library's serializer context (<see cref="JsonElement"/>, <see cref="System.Text.Json.Nodes.JsonNode"/> and the
    /// library's own models are) or in <see cref="Smtp2GoClientOptions.AdditionalJsonTypeInfoResolver"/>.
    /// </summary>
    /// <typeparam name="TRequest">The request body type.</typeparam>
    /// <typeparam name="TResponse">The type of the envelope's <c>data</c>.</typeparam>
    /// <param name="path">Path relative to the v3 base URL, for example <c>stats/email_cycle</c>.</param>
    /// <param name="body">The request body; <see langword="null"/> sends <c>{}</c>.</param>
    /// <param name="method">Overrides the method from the endpoint table (<c>POST</c> for unknown paths).</param>
    /// <param name="options">Per-call overrides.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<ApiResponse<TResponse>> SendAsync<TRequest, TResponse>(
        string path,
        TRequest? body,
        HttpMethod? method = null,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends <paramref name="body"/> to <paramref name="path"/> and returns the whole response envelope as a <see cref="JsonDocument"/>, which the caller must dispose.
    /// This is the path for undocumented endpoints and fields.
    /// </summary>
    /// <param name="path">Path relative to the v3 base URL.</param>
    /// <param name="body">The request body; <c>default</c> sends <c>{}</c>.</param>
    /// <param name="method">Overrides the method from the endpoint table.</param>
    /// <param name="options">Per-call overrides.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task<JsonDocument> SendJsonAsync(
        string path,
        JsonElement body,
        HttpMethod? method = null,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default);
}
