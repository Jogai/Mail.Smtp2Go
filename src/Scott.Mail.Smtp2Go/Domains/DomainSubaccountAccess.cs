using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>Which subaccounts may send from a sender domain of the master account: the <c>subaccount_access</c> object of <c>domain/add</c> (request) and <c>domain/view</c> (response).</summary>
public sealed record DomainSubaccountAccess
{
    /// <summary>The ids of the subaccounts with access (from <c>subaccounts/search</c>).</summary>
    [JsonPropertyName("subaccounts")]
    public IReadOnlyList<string>? Subaccounts { get; init; }

    /// <summary>Whether subaccounts created later get access automatically.</summary>
    [JsonPropertyName("future_subaccounts")]
    public bool? FutureSubaccounts { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
