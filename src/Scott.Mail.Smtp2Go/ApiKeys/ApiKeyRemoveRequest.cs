using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>api_keys/remove</c>.</summary>
[Smtp2GoEndpoint("api_keys/remove")]
internal sealed record ApiKeyRemoveRequest
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
}
