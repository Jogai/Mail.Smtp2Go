using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /subaccount/add</c> (50 per hour). Only <see cref="FullName"/> is required; the docs give the server defaults for the rest.</summary>
[Smtp2GoEndpoint("subaccount/add")]
public sealed record SubaccountAddRequest : IRequestValidator
{
    /// <summary>The subaccount's full name.</summary>
    [JsonPropertyName("fullname")]
    public required string FullName { get; init; }

    /// <summary>An address for the first team member; it receives an invitation.</summary>
    [JsonPropertyName("subaccount_email")]
    public string? SubaccountEmail { get; init; }

    /// <summary>Emails allowed per billing cycle (server default 10,000). The docs list the valid sizes: 2,000 to 10,000,000 in fixed steps.</summary>
    [JsonPropertyName("limit")]
    public int? Limit { get; init; }

    /// <summary>Assign a dedicated IP (only with a limit above 100,000). Server default <see langword="false"/>.</summary>
    [JsonPropertyName("dedicated_ip")]
    public bool? DedicatedIp { get; init; }

    /// <summary>Allow the subaccount to enable archiving. Server default <see langword="false"/>.</summary>
    [JsonPropertyName("archiving")]
    public bool? Archiving { get; init; }

    /// <summary>Require two-factor authentication of its team members. Server default <see langword="false"/>.</summary>
    [JsonPropertyName("enforce_2fa")]
    public bool? Enforce2fa { get; init; }

    /// <summary>Enable SMS messaging (additional charges apply). Server default <see langword="false"/>.</summary>
    [JsonPropertyName("enable_sms")]
    public bool? EnableSms { get; init; }

    /// <summary>The monthly SMS limit (server default 1,000), also capped by the master account's limit.</summary>
    [JsonPropertyName("sms_limit")]
    public int? SmsLimit { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        SubaccountRequestValidator.ValidateRequired(FullName, "fullname", errors);
        SubaccountRequestValidator.ValidateLimits(Limit, SmsLimit, errors);
    }
}
