using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /webhook/remove</c>.</summary>
[Smtp2GoEndpoint("webhook/remove")]
public sealed record WebhookRemoveRequest
{
    /// <summary>The id of the webhook to remove.</summary>
    [JsonPropertyName("id")]
    public required long Id { get; init; }
}
