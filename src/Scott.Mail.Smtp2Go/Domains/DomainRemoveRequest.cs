using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>domain/remove</c>.</summary>
[Smtp2GoEndpoint("domain/remove")]
internal sealed record DomainRemoveRequest
{
    [JsonPropertyName("domain")]
    public required string Domain { get; init; }
}
