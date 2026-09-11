using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>One sender domain: its verification details, tracking subdomains and, on <c>domain/view</c>, subaccount access and delegation.</summary>
public sealed record SenderDomain
{
    /// <summary>The domain and its DKIM and return-path verification.</summary>
    [JsonPropertyName("domain")]
    public DomainDetails? Domain { get; init; }

    /// <summary>The tracking subdomains and their CNAME verification.</summary>
    [JsonPropertyName("trackers")]
    public IReadOnlyList<TrackingDomain>? Trackers { get; init; }

    /// <summary>Which subaccounts may send from the domain (<c>domain/view</c> only).</summary>
    [JsonPropertyName("subaccount_access")]
    public DomainSubaccountAccess? SubaccountAccess { get; init; }

    /// <summary>Whether the domain is delegated to the subaccount from the master account (<c>domain/view</c> through a subaccount).</summary>
    [JsonPropertyName("from_master")]
    public bool? FromMaster { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
