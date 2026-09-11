using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>One tracking subdomain of a sender domain and its CNAME verification.</summary>
public sealed record TrackingDomain
{
    /// <summary>The full tracking domain, for example <c>link.example.com</c>.</summary>
    [JsonPropertyName("fulldomain")]
    public string? FullDomain { get; init; }

    /// <summary>The subdomain part, for example <c>link</c>.</summary>
    [JsonPropertyName("subdomain")]
    public string? Subdomain { get; init; }

    /// <summary>The registrable name.</summary>
    [JsonPropertyName("domain")]
    public string? Domain { get; init; }

    /// <summary>The public suffix.</summary>
    [JsonPropertyName("suffix")]
    public string? Suffix { get; init; }

    /// <summary>Whether the CNAME record verified.</summary>
    [JsonPropertyName("cname_verified")]
    public bool? CnameVerified { get; init; }

    /// <summary>The CNAME verification message, empty when fine.</summary>
    [JsonPropertyName("cname_status")]
    public string? CnameStatus { get; init; }

    /// <summary>The CNAME target, for example <c>track.smtp2go.net</c>.</summary>
    [JsonPropertyName("cname_value")]
    public string? CnameValue { get; init; }

    /// <summary>Whether tracking through this subdomain is enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
