using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The body of <c>POST /suppression/view</c>. Every field is optional. The endpoint pages by <c>continue_token</c> only; the docs list no page size.
/// <c>subaccount_id</c> is injected from <see cref="RequestOptions.SubaccountId"/>.
/// </summary>
public sealed record SuppressionViewRequest : IRequestValidator
{
    /// <summary>The token from the previous page's <see cref="SuppressionViewResult.ContinueToken"/>. <see cref="ISuppressionClient.ViewAllAsync"/> manages it.</summary>
    [JsonPropertyName("continue_token")]
    public string? ContinueToken { get; init; }

    /// <summary>Check whether this address or domain is suppressed.</summary>
    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; init; }

    /// <summary>Range start (UTC). The docs describe the server default as the current date at midnight.</summary>
    [JsonPropertyName("start_date")]
    public DateTimeOffset? StartDate { get; init; }

    /// <summary>Range end (UTC). The docs describe the server default as 30 days before the current date at midnight (sic).</summary>
    [JsonPropertyName("end_date")]
    public DateTimeOffset? EndDate { get; init; }

    /// <summary>Fuzzy matching on recipients and reasons.</summary>
    [JsonPropertyName("fuzzy")]
    public bool? Fuzzy { get; init; }

    /// <summary>A reason string to search for.</summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>Reason strings to search for.</summary>
    [JsonPropertyName("reasons")]
    public IReadOnlyList<string>? Reasons { get; init; }

    /// <summary>A recipient string to search for.</summary>
    [JsonPropertyName("recipient")]
    public string? Recipient { get; init; }

    /// <summary>Recipient strings to search for.</summary>
    [JsonPropertyName("recipients")]
    public IReadOnlyList<string>? Recipients { get; init; }

    /// <summary>Sort order.</summary>
    [JsonPropertyName("sort")]
    public SortDirection? Sort { get; init; }

    /// <summary>Restrict to one suppression type.</summary>
    [JsonPropertyName("suppression_type")]
    public SuppressionType? SuppressionType { get; init; }

    /// <summary>Restrict to these suppression types.</summary>
    [JsonPropertyName("suppression_types")]
    public IReadOnlyList<SuppressionType>? SuppressionTypes { get; init; }

    /// <summary>Only suppressions whose name, domain or address contains this.</summary>
    [JsonPropertyName("wildcard")]
    public string? Wildcard { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        if (Sort == SortDirection.Unknown)
        {
            errors.Add("sort must not be SortDirection.Unknown.");
        }

        if (SuppressionType == Smtp2Go.SuppressionType.Unknown || (SuppressionTypes is not null && SuppressionTypes.Contains(Smtp2Go.SuppressionType.Unknown)))
        {
            errors.Add("suppression_type(s) must not contain SuppressionType.Unknown.");
        }
    }
}
