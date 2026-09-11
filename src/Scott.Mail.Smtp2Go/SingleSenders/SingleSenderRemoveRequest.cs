using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>single_sender_emails/remove</c>.</summary>
[Smtp2GoEndpoint("single_sender_emails/remove")]
internal sealed record SingleSenderRemoveRequest
{
    [JsonPropertyName("email_address")]
    public required string EmailAddress { get; init; }
}
