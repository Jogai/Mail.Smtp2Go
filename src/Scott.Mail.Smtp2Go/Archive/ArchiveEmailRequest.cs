using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>archive/email</c>.</summary>
[Smtp2GoEndpoint("archive/email")]
internal sealed record ArchiveEmailRequest
{
    [JsonPropertyName("email_id")]
    public required string EmailId { get; init; }
}
