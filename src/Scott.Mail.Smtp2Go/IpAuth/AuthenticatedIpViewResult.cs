using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>ip_auth/view</c>: the entries plus the account's default rate limit.</summary>
public sealed record AuthenticatedIpViewResult
{
    /// <summary>The entries; empty when none match.</summary>
    [JsonPropertyName("results")]
    public IReadOnlyList<AuthenticatedIp>? Results { get; init; }

    /// <summary>The account's default rate-limit value (<c>0</c> when unlimited).</summary>
    [JsonPropertyName("default_ratelimit_value")]
    public int? DefaultRateLimitValue { get; init; }

    /// <summary>The account's default rate-limit period (<c>unlimited</c> when none).</summary>
    [JsonPropertyName("default_ratelimit_period")]
    public string? DefaultRateLimitPeriod { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
