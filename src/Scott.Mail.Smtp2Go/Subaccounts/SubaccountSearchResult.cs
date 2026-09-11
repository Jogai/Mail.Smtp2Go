using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>subaccounts/search</c>: one page of subaccounts.</summary>
public sealed record SubaccountSearchResult
{
    /// <summary>The subaccounts of this page.</summary>
    [JsonPropertyName("subaccounts")]
    public IReadOnlyList<Subaccount>? Subaccounts { get; init; }

    /// <summary>Pass as <see cref="SubaccountSearchRequest.ContinueToken"/> for the next page; <see langword="null"/> or empty on the last page.</summary>
    [JsonPropertyName("continue_token")]
    public string? ContinueToken { get; init; }

    /// <summary>The number of subaccounts matching the search.</summary>
    [JsonPropertyName("total_count")]
    public long? TotalCount { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
