using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /webhook/remove</c>.</summary>
public sealed record WebhookRemoveRequest
{
    /// <summary>The id of the webhook to remove.</summary>
    [JsonPropertyName("id")]
    public required long Id { get; init; }
}
