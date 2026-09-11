using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /template/search</c>. Every field is optional; without filters every template is listed, <see cref="PageSize"/> (server default 100) at a time.</summary>
[Smtp2GoEndpoint("template/search")]
public sealed record TemplateSearchRequest : IRequestValidator
{
    /// <summary>Match <see cref="SearchTerms"/> with wildcards rather than exactly. Server default <see langword="false"/>.</summary>
    [JsonPropertyName("fuzzy_search")]
    public bool? FuzzySearch { get; init; }

    /// <summary>Return templates whose name, tag, id or subject contains any of these.</summary>
    [JsonPropertyName("search_terms")]
    public IReadOnlyList<string>? SearchTerms { get; init; }

    /// <summary>Return templates carrying any of these tags.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>Sort order. Server default ascending.</summary>
    [JsonPropertyName("sort_direction")]
    public SortDirection? SortDirection { get; init; }

    /// <summary>Page size. Server default 100.</summary>
    [JsonPropertyName("page_size")]
    public int? PageSize { get; init; }

    /// <summary>The token from the previous page's <see cref="TemplateSearchResult.ContinueToken"/>. <see cref="ITemplateClient.SearchAllAsync"/> manages it.</summary>
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
    }
}
