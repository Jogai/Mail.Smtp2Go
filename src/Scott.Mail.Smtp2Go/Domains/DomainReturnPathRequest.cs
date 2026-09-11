using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /domain/returnpath</c>: renames a sender domain's return-path subdomain (the docs' example goes from <c>returns</c> to <c>return</c>). <c>subaccount_id</c> is injected from <see cref="RequestOptions.SubaccountId"/>.</summary>
[Smtp2GoEndpoint("domain/returnpath")]
public sealed record DomainReturnPathRequest : IRequestValidator
{
    /// <summary>The sender domain.</summary>
    [JsonPropertyName("domain")]
    public required string Domain { get; init; }

    /// <summary>The current return-path subdomain.</summary>
    [JsonPropertyName("old_subdomain")]
    public required string OldSubdomain { get; init; }

    /// <summary>The new return-path subdomain.</summary>
    [JsonPropertyName("new_subdomain")]
    public required string NewSubdomain { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        DomainRequestValidator.ValidateDomain(Domain, errors);
        DomainRequestValidator.ValidateSubdomains(OldSubdomain, NewSubdomain, errors);
    }
}
