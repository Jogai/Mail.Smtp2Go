using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /suppression/remove</c>: the address or domain and which suppression types to lift. <c>subaccount_id</c> is injected from <see cref="RequestOptions.SubaccountId"/>.</summary>
[Smtp2GoEndpoint("suppression/remove")]
public sealed record SuppressionRemoveRequest : IRequestValidator
{
    /// <summary>The address or domain to unsuppress.</summary>
    [JsonPropertyName("email_address")]
    public required string EmailAddress { get; init; }

    /// <summary>The suppression types to remove, for example <c>[Manual, Spam]</c>.</summary>
    [JsonPropertyName("reasons")]
    public required IReadOnlyList<SuppressionType> Reasons { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        if (string.IsNullOrWhiteSpace(EmailAddress))
        {
            errors.Add("email_address is required.");
        }

        if (Reasons is null || Reasons.Count == 0)
        {
            errors.Add("reasons must list at least one suppression type.");
        }
        else if (Reasons.Contains(SuppressionType.Unknown))
        {
            errors.Add("reasons must not contain SuppressionType.Unknown.");
        }
    }
}
