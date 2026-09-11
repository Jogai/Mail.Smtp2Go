using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /domain/view</c>. Without <see cref="Domain"/> every sender domain is listed. <c>subaccount_id</c> is injected from <see cref="RequestOptions.SubaccountId"/>.</summary>
[Smtp2GoEndpoint("domain/view")]
public sealed record DomainViewRequest
{
    /// <summary>Only return this domain.</summary>
    [JsonPropertyName("domain")]
    public string? Domain { get; init; }
}
