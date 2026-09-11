using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /domain/tracking</c>: renames a sender domain's tracking subdomain (the docs' example goes from <c>track</c> to <c>link</c>). <c>subaccount_id</c> is injected from <see cref="RequestOptions.SubaccountId"/>.</summary>
[Smtp2GoEndpoint("domain/tracking")]
public sealed record DomainTrackingRequest : IRequestValidator
{
    /// <summary>The sender domain.</summary>
    [JsonPropertyName("domain")]
    public required string Domain { get; init; }

    /// <summary>The current tracking subdomain.</summary>
    [JsonPropertyName("old_subdomain")]
    public required string OldSubdomain { get; init; }

    /// <summary>The new tracking subdomain.</summary>
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
