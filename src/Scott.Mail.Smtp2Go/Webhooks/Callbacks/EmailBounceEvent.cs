using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go.Webhooks;

/// <summary><c>bounce</c>: the recipient server refused the email. <see cref="EmailWebhookEvent.Host"/>, <see cref="EmailWebhookEvent.Message"/> and <see cref="EmailWebhookEvent.Context"/> describe the refusal.</summary>
public sealed record EmailBounceEvent : EmailWebhookEvent
{
    /// <summary>The <c>bounce</c> field: hard or soft.</summary>
    public BounceType? BounceType { get; init; }

    /// <summary>The <c>bounce</c> field as it arrived.</summary>
    [JsonPropertyName("bounce")]
    public string? BounceRaw { get; init; }
}
