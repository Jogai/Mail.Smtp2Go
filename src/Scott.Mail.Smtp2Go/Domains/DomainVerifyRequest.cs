using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /domain/verify</c>. <c>subaccount_id</c> is injected from <see cref="RequestOptions.SubaccountId"/>.</summary>
[Smtp2GoEndpoint("domain/verify")]
public sealed record DomainVerifyRequest : IRequestValidator
{
    /// <summary>The domain to verify.</summary>
    [JsonPropertyName("domain")]
    public required string Domain { get; init; }

    /// <summary>Request an SSL certificate for the tracking domain once verified. Server default <see langword="true"/>.</summary>
    [JsonPropertyName("requisition_ssl")]
    public bool? RequisitionSsl { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        DomainRequestValidator.ValidateDomain(Domain, errors);
    }
}
