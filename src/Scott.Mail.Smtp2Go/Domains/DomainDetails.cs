using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>domain</c> object of a sender domain: its parts, the DKIM and return-path DNS records and whether they verified.</summary>
public sealed record DomainDetails
{
    /// <summary>The full domain, for example <c>example.com</c>.</summary>
    [JsonPropertyName("fulldomain")]
    public string? FullDomain { get; init; }

    /// <summary>The subdomain part, empty or <see langword="null"/> for an apex domain.</summary>
    [JsonPropertyName("subdomain")]
    public string? Subdomain { get; init; }

    /// <summary>The registrable name, for example <c>example</c>.</summary>
    [JsonPropertyName("domain")]
    public string? Domain { get; init; }

    /// <summary>The public suffix, for example <c>com</c> or <c>co.uk</c>.</summary>
    [JsonPropertyName("suffix")]
    public string? Suffix { get; init; }

    /// <summary>The DKIM selector to publish.</summary>
    [JsonPropertyName("dkim_selector")]
    public string? DkimSelector { get; init; }

    /// <summary>Whether the DKIM record verified.</summary>
    [JsonPropertyName("dkim_verified")]
    public bool? DkimVerified { get; init; }

    /// <summary>The DKIM verification message, empty when fine.</summary>
    [JsonPropertyName("dkim_status")]
    public string? DkimStatus { get; init; }

    /// <summary>The CNAME target of the DKIM record.</summary>
    [JsonPropertyName("dkim_value")]
    public string? DkimValue { get; init; }

    /// <summary>The return-path subdomain.</summary>
    [JsonPropertyName("rpath_selector")]
    public string? ReturnPathSelector { get; init; }

    /// <summary>Whether the return-path record verified.</summary>
    [JsonPropertyName("rpath_verified")]
    public bool? ReturnPathVerified { get; init; }

    /// <summary>The return-path verification message, empty when fine.</summary>
    [JsonPropertyName("rpath_status")]
    public string? ReturnPathStatus { get; init; }

    /// <summary>The CNAME target of the return-path record.</summary>
    [JsonPropertyName("rpath_value")]
    public string? ReturnPathValue { get; init; }

    /// <summary>A URL that automates the DNS additions; only present when a single domain is returned (otherwise <c>unavailable</c>).</summary>
    [JsonPropertyName("setup_link")]
    public string? SetupLink { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
