using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /ip_auth/view</c>. Without <see cref="IpAddress"/> every entry is listed. <c>subaccount_id</c> is injected from <see cref="RequestOptions.SubaccountId"/>.</summary>
[Smtp2GoEndpoint("ip_auth/view")]
public sealed record AuthenticatedIpViewRequest
{
    /// <summary>The address of the entry to view.</summary>
    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; init; }
}
