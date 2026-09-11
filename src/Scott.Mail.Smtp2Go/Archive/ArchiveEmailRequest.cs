using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>archive/email</c>.</summary>
internal sealed record ArchiveEmailRequest
{
    [JsonPropertyName("email_id")]
    public required string EmailId { get; init; }
}
