using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /subaccount/edit</c>: the <see cref="Id"/> plus the fields to change. The docs describe the boolean fields with defaults, so pass every setting you care about.</summary>
[Smtp2GoEndpoint("subaccount/edit")]
public sealed record SubaccountEditRequest : IRequestValidator
{
    /// <summary>The id of the subaccount to change.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>A new full name.</summary>
    [JsonPropertyName("fullname")]
    public string? FullName { get; init; }

    /// <summary>A new limit of emails per billing cycle.</summary>
    [JsonPropertyName("limit")]
    public int? Limit { get; init; }

    /// <summary>Assign a dedicated IP (only with a limit above 100,000).</summary>
    [JsonPropertyName("dedicated_ip")]
    public bool? DedicatedIp { get; init; }

    /// <summary>Allow the subaccount to enable archiving.</summary>
    [JsonPropertyName("archiving")]
    public bool? Archiving { get; init; }

    /// <summary>Require two-factor authentication of its team members.</summary>
    [JsonPropertyName("enforce_2fa")]
    public bool? Enforce2fa { get; init; }

    /// <summary>Enable SMS messaging (additional charges apply).</summary>
    [JsonPropertyName("enable_sms")]
    public bool? EnableSms { get; init; }

    /// <summary>The monthly SMS limit.</summary>
    [JsonPropertyName("sms_limit")]
    public int? SmsLimit { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        SubaccountRequestValidator.ValidateRequired(Id, "id", errors);
        SubaccountRequestValidator.ValidateLimits(Limit, SmsLimit, errors);
    }
}
