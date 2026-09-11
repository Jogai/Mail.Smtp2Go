using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /users/smtp/view</c>. Without <see cref="Username"/> every user is listed. <c>subaccount_id</c> is injected from <see cref="RequestOptions.SubaccountId"/>.</summary>
[Smtp2GoEndpoint("users/smtp/view")]
public sealed record SmtpUserViewRequest
{
    /// <summary>The username to view.</summary>
    [JsonPropertyName("username")]
    public string? Username { get; init; }
}
