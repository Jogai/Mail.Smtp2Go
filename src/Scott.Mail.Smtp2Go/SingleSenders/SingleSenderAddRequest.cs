using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /single_sender_emails/add</c>. <c>subaccount_id</c> is injected from <see cref="RequestOptions.SubaccountId"/>.</summary>
[Smtp2GoEndpoint("single_sender_emails/add")]
public sealed record SingleSenderAddRequest : IRequestValidator
{
    /// <summary>The address to send from; it receives the verification email.</summary>
    [JsonPropertyName("email_address")]
    public required string EmailAddress { get; init; }

    /// <summary>A plain-text message to include in the verification email.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        if (EmailAddress is null || EmailAddress.Trim().Length == 0)
        {
            errors.Add("email_address is required.");
        }
        else if (EmailAddress.IndexOf('@') < 1)
        {
            errors.Add("email_address must be an email address.");
        }
    }
}
