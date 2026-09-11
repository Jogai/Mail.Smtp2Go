using System.Text.Json;
using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>One dedicated IP pool from <c>dedicated_ips/view</c>.</summary>
[Smtp2GoEndpoint("dedicated_ips/view")]
public sealed record DedicatedIpPool
{
    /// <summary>The pool id; pass it as <c>ip_pool</c> when creating or editing a sending credential.</summary>
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    /// <summary>The pool name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>The dedicated addresses in the pool.</summary>
    [JsonPropertyName("ip_addresses")]
    public IReadOnlyList<string>? IpAddresses { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
