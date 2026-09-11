using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>domain/subaccount_access</c>: the domain and the access it now grants.</summary>
public sealed record DomainSubaccountAccessResult
{
    /// <summary>The sender domain that was changed.</summary>
    [JsonPropertyName("domain")]
    public string? Domain { get; init; }

    /// <summary>The ids of the subaccounts with access.</summary>
    [JsonPropertyName("subaccounts")]
    public IReadOnlyList<string>? Subaccounts { get; init; }

    /// <summary>Whether subaccounts created later get access automatically.</summary>
    [JsonPropertyName("future_subaccounts")]
    public bool? FutureSubaccounts { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
