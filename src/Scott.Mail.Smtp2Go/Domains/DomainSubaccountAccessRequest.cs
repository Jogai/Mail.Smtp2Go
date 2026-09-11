using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /domain/subaccount_access</c>: which subaccounts may send from a verified domain of the master account. The endpoint does not document <c>subaccount_id</c>.</summary>
[Smtp2GoEndpoint("domain/subaccount_access")]
public sealed record DomainSubaccountAccessRequest : IRequestValidator
{
    /// <summary>The verified sender domain.</summary>
    [JsonPropertyName("domain")]
    public required string Domain { get; init; }

    /// <summary>The ids of the subaccounts to give access (from <c>subaccounts/search</c>); an empty list revokes it.</summary>
    [JsonPropertyName("subaccounts")]
    public required IReadOnlyList<string> Subaccounts { get; init; }

    /// <summary>Also give access to subaccounts created later. Server default <see langword="false"/>.</summary>
    [JsonPropertyName("future_subaccounts")]
    public bool? FutureSubaccounts { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        DomainRequestValidator.ValidateDomain(Domain, errors);
        if (Subaccounts is null)
        {
            errors.Add("subaccounts is required (an empty list revokes access).");
        }
        else
        {
            DomainRequestValidator.ValidateSubaccounts(Subaccounts, errors);
        }
    }
}
