using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>ip_auth/remove</c>.</summary>
[Smtp2GoEndpoint("ip_auth/remove")]
internal sealed record AuthenticatedIpRemoveRequest
{
    [JsonPropertyName("ip_address")]
    public required string IpAddress { get; init; }
}
