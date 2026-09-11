using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /api_keys/view</c>. Both filters are optional; without them every key is listed. <c>subaccount_id</c> is injected from <see cref="RequestOptions.SubaccountId"/>.</summary>
[Smtp2GoEndpoint("api_keys/view")]
public sealed record ApiKeyViewRequest
{
    /// <summary>A full API key to retrieve.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>A keyword to refine the results.</summary>
    [JsonPropertyName("search")]
    public string? Search { get; init; }
}
