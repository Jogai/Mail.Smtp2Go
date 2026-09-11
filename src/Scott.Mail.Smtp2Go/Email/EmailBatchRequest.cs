using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The body of <c>POST /email/batch</c>: up to 1,000 <see cref="EmailSendRequest"/> objects sent in one call. Each may carry its own
/// <see cref="EmailSendRequest.Schedule"/>; the response lists one <see cref="EmailBatchItem"/> per email in request order. The documented
/// per-email fields do not include <c>fastaccept</c>, so <see cref="Smtp2GoClientOptions.DefaultFastAccept"/> is not applied to batch items.
/// </summary>
public sealed record EmailBatchRequest
{
    /// <summary>The documented maximum number of emails per call.</summary>
    public const int MaxEmails = 1000;

    /// <summary>The emails to send, 1 to 1,000.</summary>
    [JsonPropertyName("emails")]
    public required IReadOnlyList<EmailSendRequest> Emails { get; init; }
}
