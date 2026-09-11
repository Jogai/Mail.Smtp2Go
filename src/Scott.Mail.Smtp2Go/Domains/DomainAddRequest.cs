using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /domain/add</c>. Only <see cref="Domain"/> is required; the server verifies immediately and requests an SSL certificate for the tracking domain unless told otherwise. <c>subaccount_id</c> is injected from <see cref="RequestOptions.SubaccountId"/>.</summary>
[Smtp2GoEndpoint("domain/add")]
public sealed record DomainAddRequest : IRequestValidator
{
    /// <summary>The domain to add; you must own it and publish its DNS records.</summary>
    [JsonPropertyName("domain")]
    public required string Domain { get; init; }

    /// <summary>The subdomain for click and open tracking and unsubscribe links (server default <c>link</c>).</summary>
    [JsonPropertyName("tracking_subdomain")]
    public string? TrackingSubdomain { get; init; }

    /// <summary>The return-path subdomain (server default <c>return</c>).</summary>
    [JsonPropertyName("returnpath_subdomain")]
    public string? ReturnPathSubdomain { get; init; }

    /// <summary>Verify now instead of waiting for the periodic check; the DNS records must already have propagated. Server default <see langword="true"/>.</summary>
    [JsonPropertyName("auto_verify")]
    public bool? AutoVerify { get; init; }

    /// <summary>Request an SSL certificate for the tracking domain once verified. Server default <see langword="true"/>.</summary>
    [JsonPropertyName("requisition_ssl")]
    public bool? RequisitionSsl { get; init; }

    /// <summary>Which subaccounts may send from the domain (master-account keys).</summary>
    [JsonPropertyName("subaccount_access")]
    public DomainSubaccountAccess? SubaccountAccess { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        DomainRequestValidator.ValidateDomain(Domain, errors);
        DomainRequestValidator.ValidateSubaccounts(SubaccountAccess?.Subaccounts, errors);
    }
}
