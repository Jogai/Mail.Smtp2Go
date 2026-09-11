using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /suppression/add</c>. <c>subaccount_id</c> is injected from <see cref="RequestOptions.SubaccountId"/>.</summary>
[Smtp2GoEndpoint("suppression/add")]
public sealed record SuppressionAddRequest : IRequestValidator
{
    /// <summary>The address or domain to suppress.</summary>
    [JsonPropertyName("email_address")]
    public required string EmailAddress { get; init; }

    /// <summary>Why it is suppressed.</summary>
    [JsonPropertyName("block_description")]
    public string? BlockDescription { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        if (string.IsNullOrWhiteSpace(EmailAddress))
        {
            errors.Add("email_address is required.");
        }
    }
}
