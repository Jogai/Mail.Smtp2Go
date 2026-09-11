using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// One API key as returned by <c>api_keys/view</c>, <c>api_keys/add</c> and <c>api_keys/edit</c>. <c>add</c> returns the key in full; the other
/// endpoints mask it (<c>api-000000000000********************</c>). The documented examples carry a subset of these fields; the rest mirror the
/// settings of <see cref="ApiKeyAddRequest"/> and land here when the server returns them, or in <see cref="Extra"/> under another name.
/// </summary>
public sealed record ApiKey
{
    /// <summary>The key, unmasked only in the <c>api_keys/add</c> response.</summary>
    [JsonPropertyName("api_key")]
    public string? Key { get; init; }

    /// <summary>The key's description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The key's status.</summary>
    [JsonPropertyName("status")]
    public CredentialStatus? Status { get; init; }

    /// <summary>The endpoints the key may call, for example <c>/email/send</c> or <c>/email/*</c>.</summary>
    [JsonPropertyName("endpoints")]
    public IReadOnlyList<string>? Endpoints { get; init; }

    /// <summary>Whether sending is currently allowed.</summary>
    [JsonPropertyName("sending_allowed")]
    public bool? SendingAllowed { get; init; }

    /// <summary>The username associated with the key, where returned.</summary>
    [JsonPropertyName("username")]
    public string? Username { get; init; }

    /// <summary>Whether a custom rate limit applies.</summary>
    [JsonPropertyName("custom_ratelimit")]
    public bool? CustomRateLimit { get; init; }

    /// <summary>Emails allowed per <see cref="CustomRateLimitPeriod"/>.</summary>
    [JsonPropertyName("custom_ratelimit_value")]
    public int? CustomRateLimitValue { get; init; }

    /// <summary>The custom rate-limit period, for example <c>1 day</c> or <c>0:30:00</c>.</summary>
    [JsonPropertyName("custom_ratelimit_period")]
    public string? CustomRateLimitPeriod { get; init; }

    /// <summary>The account's default rate-limit value, where returned.</summary>
    [JsonPropertyName("default_ratelimit_value")]
    public int? DefaultRateLimitValue { get; init; }

    /// <summary>The account's default rate-limit period, where returned (<c>unlimited</c> when none).</summary>
    [JsonPropertyName("default_ratelimit_period")]
    public string? DefaultRateLimitPeriod { get; init; }

    /// <summary>The dedicated IP pool the key sends from (<c>ippool</c> on the wire; <c>ip_pool</c> in requests).</summary>
    [JsonPropertyName("ippool")]
    public int? IpPool { get; init; }

    /// <summary>Whether custom feedback via the unsubscribe footer is enabled.</summary>
    [JsonPropertyName("feedback_enabled")]
    public bool? FeedbackEnabled { get; init; }

    /// <summary>The feedback domain, where returned.</summary>
    [JsonPropertyName("feedback_domain")]
    public string? FeedbackDomain { get; init; }

    /// <summary>The HTML inserted into the feedback email.</summary>
    [JsonPropertyName("feedback_html")]
    public string? FeedbackHtml { get; init; }

    /// <summary>The text inserted into the feedback email.</summary>
    [JsonPropertyName("feedback_text")]
    public string? FeedbackText { get; init; }

    /// <summary>Whether open tracking is enabled.</summary>
    [JsonPropertyName("open_tracking_enabled")]
    public bool? OpenTrackingEnabled { get; init; }

    /// <summary>Whether click tracking is enabled.</summary>
    [JsonPropertyName("click_tracking_enabled")]
    public bool? ClickTrackingEnabled { get; init; }

    /// <summary>Whether archiving is enabled.</summary>
    [JsonPropertyName("archive_enabled")]
    public bool? ArchiveEnabled { get; init; }

    /// <summary>The address BCC'd on every email sent with the key.</summary>
    [JsonPropertyName("audit_email")]
    public string? AuditEmail { get; init; }

    /// <summary>How bounce notifications are handled.</summary>
    [JsonPropertyName("bounce_notifications")]
    public BounceNotifications? BounceNotifications { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
