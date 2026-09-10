using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The envelope every SMTP2GO v3 response uses: a <c>request_id</c> and a <c>data</c> payload.</summary>
/// <typeparam name="TData">The shape of <c>data</c>: an object for most endpoints, an array for list endpoints, or <see cref="JsonElement"/> for raw calls.</typeparam>
public sealed class ApiResponse<TData>
{
    /// <summary>The server-assigned request id. Quote it when contacting SMTP2GO support.</summary>
    [JsonPropertyName("request_id")]
    public required string RequestId { get; init; }

    /// <summary>The endpoint's payload.</summary>
    [JsonPropertyName("data")]
    public required TData Data { get; init; }

    /// <summary>Any top-level field this library does not model, so new server fields are observable before the model is updated.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; init; }
}
