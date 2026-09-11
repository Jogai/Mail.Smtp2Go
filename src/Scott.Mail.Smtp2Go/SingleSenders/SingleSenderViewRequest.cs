using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /single_sender_emails/view</c>. Without <see cref="EmailAddress"/> every address is listed. <c>subaccount_id</c> is injected from <see cref="RequestOptions.SubaccountId"/>.</summary>
[Smtp2GoEndpoint("single_sender_emails/view")]
public sealed record SingleSenderViewRequest
{
    /// <summary>Only return addresses matching this value.</summary>
    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; init; }
}
