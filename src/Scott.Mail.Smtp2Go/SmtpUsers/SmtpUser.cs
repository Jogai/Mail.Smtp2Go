using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// One SMTP user as returned by the <c>users/smtp/*</c> endpoints. <c>view</c>, <c>add</c>, <c>edit</c> (POST) and <c>remove</c> wrap the users in
/// <c>{"results": [...]}</c>, <c>edit</c> (PATCH) returns a bare array; <see cref="Json.Converters.ResultsListConverter{TItem}"/> reads both. The
/// password is returned in clear text.
/// </summary>
public sealed record SmtpUser
{
    /// <summary>The username.</summary>
    [JsonPropertyName("username")]
    public string? Username { get; init; }

    /// <summary>The SMTP password, in clear text.</summary>
    [JsonPropertyName("email_password")]
    public string? EmailPassword { get; init; }

    /// <summary>The user's description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The user's status.</summary>
    [JsonPropertyName("status")]
    public CredentialStatus? Status { get; init; }

    /// <summary>Whether sending is currently allowed.</summary>
    [JsonPropertyName("sending_allowed")]
    public bool? SendingAllowed { get; init; }

    /// <summary>Whether a custom rate limit applies.</summary>
    [JsonPropertyName("custom_ratelimit")]
    public bool? CustomRateLimit { get; init; }

    /// <summary>Emails allowed per <see cref="CustomRateLimitPeriod"/>.</summary>
    [JsonPropertyName("custom_ratelimit_value")]
    public int? CustomRateLimitValue { get; init; }

    /// <summary>The custom rate-limit period, for example <c>1 day</c> or <c>0:00:00</c>.</summary>
    [JsonPropertyName("custom_ratelimit_period")]
    public string? CustomRateLimitPeriod { get; init; }

    /// <summary>The account's default rate-limit value, where returned per user.</summary>
    [JsonPropertyName("default_ratelimit_value")]
    public int? DefaultRateLimitValue { get; init; }

    /// <summary>The account's default rate-limit period, where returned per user.</summary>
    [JsonPropertyName("default_ratelimit_period")]
    public string? DefaultRateLimitPeriod { get; init; }

    /// <summary>The dedicated IP pool the user sends from (<c>ippool</c> on the wire; <c>ip_pool</c> in requests).</summary>
    [JsonPropertyName("ippool")]
    public int? IpPool { get; init; }

    /// <summary>Whether custom feedback via the unsubscribe footer is enabled.</summary>
    [JsonPropertyName("feedback_enabled")]
    public bool? FeedbackEnabled { get; init; }

    /// <summary>The domain used in feedback links (<c>default</c> for SMTP2GO's).</summary>
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

    /// <summary>The address BCC'd on every email the user sends.</summary>
    [JsonPropertyName("audit_email")]
    public string? AuditEmail { get; init; }

    /// <summary>How bounce notifications are handled.</summary>
    [JsonPropertyName("bounce_notifications")]
    public BounceNotifications? BounceNotifications { get; init; }

    /// <summary>Free-text comments, where returned.</summary>
    [JsonPropertyName("comments")]
    public string? Comments { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
