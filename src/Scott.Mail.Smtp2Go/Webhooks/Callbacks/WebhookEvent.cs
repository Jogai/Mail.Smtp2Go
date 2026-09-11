using System.Text.Json;

namespace Scott.Mail.Smtp2Go.Webhooks;

/// <summary>
/// Base of every parsed callback. <c>WebhookPayloadParser</c> returns the concrete subtype for <see cref="Kind"/>; switch on the type or on
/// <see cref="Kind"/>. Fields the subtype does not model are kept in <see cref="Extra"/> so nothing the API sends is lost.
/// </summary>
public abstract record WebhookEvent
{
    /// <summary>The resolved kind.</summary>
    public required WebhookEventKind Kind { get; init; }

    /// <summary>The <c>event</c> value exactly as it arrived, for example <c>open</c> or <c>opened</c>. Empty when the payload had no <c>event</c>.</summary>
    public required string EventRaw { get; init; }

    /// <summary>The <c>id</c> field: the unique id of this callback delivery.</summary>
    public string? WebhookId { get; init; }

    /// <summary>The <c>time</c> field: when the event happened (UTC). For SMS events this is <c>received_timestamp</c> when <c>time</c> is absent.</summary>
    public DateTimeOffset? Time { get; init; }

    /// <summary>Every field the subtype does not model and that is not a declared custom header, keyed by the original wire name. <see langword="null"/> when there are none.</summary>
    public IReadOnlyDictionary<string, JsonElement>? Extra { get; init; }
}
