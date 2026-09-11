using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /subaccounts/search</c>. Every field is optional; without filters every subaccount is listed, <see cref="PageSize"/> (server default 100) at a time.</summary>
[Smtp2GoEndpoint("subaccounts/search")]
public sealed record SubaccountSearchRequest : IRequestValidator
{
    /// <summary>Match <see cref="SearchTerms"/> partially and case-insensitively (<see langword="true"/>, the server default) or exactly and case-sensitively.</summary>
    [JsonPropertyName("fuzzy_search")]
    public bool? FuzzySearch { get; init; }

    /// <summary>Return subaccounts matching any of these strings.</summary>
    [JsonPropertyName("search_terms")]
    public IReadOnlyList<string>? SearchTerms { get; init; }

    /// <summary>Restrict to one state; the server default is <see cref="SubaccountStateFilter.All"/>.</summary>
    [JsonPropertyName("states")]
    public SubaccountStateFilter? States { get; init; }

    /// <summary>Sort order by subaccount name. Server default ascending.</summary>
    [JsonPropertyName("sort_direction")]
    public SortDirection? SortDirection { get; init; }

    /// <summary>Page size. Server default 100.</summary>
    [JsonPropertyName("page_size")]
    public int? PageSize { get; init; }

    /// <summary>The token from the previous page's <see cref="SubaccountSearchResult.ContinueToken"/>. <see cref="ISubaccountClient.SearchAllAsync"/> manages it.</summary>
    [JsonPropertyName("continue_token")]
    public string? ContinueToken { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        if (PageSize is <= 0)
        {
            errors.Add("page_size must be positive.");
        }

        if (SortDirection == Smtp2Go.SortDirection.Unknown)
        {
            errors.Add("sort_direction must not be SortDirection.Unknown.");
        }

        if (States == SubaccountStateFilter.Unknown)
        {
            errors.Add("states must not be SubaccountStateFilter.Unknown.");
        }
    }
}
