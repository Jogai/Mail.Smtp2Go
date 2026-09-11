namespace Scott.Mail.Smtp2Go.Webhooks;

/// <summary>Settings for <c>WebhookPayloadParser</c>.</summary>
public sealed class WebhookParserOptions
{
    /// <summary>
    /// The custom header names requested through the webhook's <c>headers</c> setting. A payload field matching one of these (case-insensitive, <c>-</c> and <c>_</c> equivalent)
    /// lands in <see cref="EmailWebhookEvent.CustomHeaders"/> under the declared name; any other unmodelled field lands in <see cref="WebhookEvent.Extra"/>.
    /// </summary>
    public IReadOnlyCollection<string> KnownCustomHeaders { get; init; } = [];
}
