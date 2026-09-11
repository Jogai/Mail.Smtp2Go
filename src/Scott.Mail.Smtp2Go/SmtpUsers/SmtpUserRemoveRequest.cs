using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>users/smtp/remove</c>.</summary>
[Smtp2GoEndpoint("users/smtp/remove")]
internal sealed record SmtpUserRemoveRequest
{
    [JsonPropertyName("username")]
    public required string Username { get; init; }
}
