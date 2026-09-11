using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>PATCH /api_keys/edit</c>: a partial edit. Omitted fields are left unchanged. Same shape as <see cref="ApiKeyEditRequest"/>; the client sends it with <c>PATCH</c>.</summary>
public sealed record ApiKeyPatchRequest : IRequestValidator
{
    /// <summary>The full API key to change.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>A comment or description of the key.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>Enables a custom rate limit defined by <see cref="CustomRateLimitValue"/> and <see cref="CustomRateLimitPeriod"/>.</summary>
    [JsonPropertyName("custom_ratelimit")]
    public bool? CustomRateLimit { get; init; }

    /// <summary>Emails allowed per <see cref="CustomRateLimitPeriod"/>. Stored only when <see cref="CustomRateLimit"/> is <see langword="true"/>.</summary>
    [JsonPropertyName("custom_ratelimit_value")]
    public int? CustomRateLimitValue { get; init; }

    /// <summary>The rate-limit period: <c>"&lt;n&gt; [hour[s]|day[s]|week[s]|month[s]] [hh:mm:ss]"</c>, for example <c>0:30:00</c>, <c>1 hour</c>, <c>2 days</c> or <c>4 months 5:00:00</c>.</summary>
    [JsonPropertyName("custom_ratelimit_period")]
    public string? CustomRateLimitPeriod { get; init; }

    /// <summary>The dedicated IP pool to send from; ids come from <c>dedicated_ips/view</c>.</summary>
    [JsonPropertyName("ip_pool")]
    public int? IpPool { get; init; }

    /// <summary>Enables custom feedback via the unsubscribe footer (<see cref="FeedbackHtml"/>, <see cref="FeedbackText"/>).</summary>
    [JsonPropertyName("feedback_enabled")]
    public bool? FeedbackEnabled { get; init; }

    /// <summary>HTML for the feedback email; <c>%UNSUBSCRIBE%</c> and <c>%EMAIL%</c> are substituted.</summary>
    [JsonPropertyName("feedback_html")]
    public string? FeedbackHtml { get; init; }

    /// <summary>Text for the feedback email; <c>%UNSUBSCRIBE%</c> and <c>%EMAIL%</c> are substituted.</summary>
    [JsonPropertyName("feedback_text")]
    public string? FeedbackText { get; init; }

    /// <summary>Enables open tracking.</summary>
    [JsonPropertyName("open_tracking_enabled")]
    public bool? OpenTrackingEnabled { get; init; }

    /// <summary>Enables click tracking.</summary>
    [JsonPropertyName("click_tracking_enabled")]
    public bool? ClickTrackingEnabled { get; init; }

    /// <summary>Enables archiving (paid plans).</summary>
    [JsonPropertyName("archive_enabled")]
    public bool? ArchiveEnabled { get; init; }

    /// <summary>An address to BCC on every email sent with the key.</summary>
    [JsonPropertyName("audit_email")]
    public string? AuditEmail { get; init; }

    /// <summary>How bounce notifications are handled: <see cref="Smtp2Go.BounceNotifications.From"/> (server default), <see cref="Smtp2Go.BounceNotifications.Drop"/> or <see cref="Smtp2Go.BounceNotifications.Email"/>.</summary>
    [JsonPropertyName("bounce_notifications")]
    public BounceNotifications? BounceNotifications { get; init; }

    /// <summary>The key's status; the server default is <see cref="CredentialStatus.Allowed"/>.</summary>
    [JsonPropertyName("status")]
    public CredentialStatus? Status { get; init; }

    /// <summary>
    /// The endpoints the key may call, for example <c>["/email/send"]</c> (the server default), wildcards such as <c>["/email/*"]</c>, or <c>["*"]</c> for everything.
    /// Must be a subset of what the calling key may use; the list is available from <see cref="IApiKeyClient.GetPermissionsAsync"/>.
    /// </summary>
    [JsonPropertyName("endpoints")]
    public IReadOnlyList<string>? Endpoints { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        CredentialRequestValidator.ValidateRequired(Id, "id", errors);
        CredentialRequestValidator.ValidateSettings(Status, CustomRateLimitValue, CustomRateLimitPeriod, errors);
        CredentialRequestValidator.ValidateEndpoints(Endpoints, errors);
    }
}
