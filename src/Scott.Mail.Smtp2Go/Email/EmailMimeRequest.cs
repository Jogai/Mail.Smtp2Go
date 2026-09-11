using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The body of <c>POST /email/mime</c>: a complete, Base64-encoded MIME message (for example one produced by MimeKit's <c>MimeMessage.WriteTo</c>).
/// Recipients, subject and bodies come from the MIME headers; only scheduling and <c>fastaccept</c> are separate fields.
/// </summary>
[Smtp2GoEndpoint("email/mime")]
public sealed record EmailMimeRequest : IRequestValidator
{
    /// <summary>The raw MIME message, Base64-encoded.</summary>
    [JsonPropertyName("mime_email")]
    public required string MimeEmail { get; init; }

    /// <summary>When to send: must be in the future and at most three days ahead. Sent as ISO-8601 UTC.</summary>
    [JsonPropertyName("schedule")]
    public DateTimeOffset? Schedule { get; init; }

    /// <summary>Accept immediately and send in the background. <see langword="null"/> uses <see cref="Smtp2GoClientOptions.DefaultFastAccept"/>, then the server default.</summary>
    [JsonPropertyName("fastaccept")]
    public bool? FastAccept { get; init; }

    /// <summary>Creates a request from the raw bytes of a MIME message, Base64-encoding them.</summary>
    public static EmailMimeRequest FromBytes(byte[] mimeMessage)
    {
        Argument.ThrowIfNull(mimeMessage);
        return new EmailMimeRequest { MimeEmail = Convert.ToBase64String(mimeMessage) };
    }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        EmailRequestValidator.ValidateMime(this, errors);
    }
}
