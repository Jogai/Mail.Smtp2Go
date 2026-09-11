namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The <c>stats/*</c> family. The summary, bounce, spam and unsubscribe reports take only an optional <c>username</c> filter (they cover the billing cycle or the
/// last 30 days; the API documents no date range for them). Only <c>email_history</c> takes dates and grouping.
/// </summary>
public interface IStatsClient
{
    /// <summary><c>POST /stats/email_summary</c>: cycle, bounce, spam and unsubscribe figures in one call. Slower than the individual reports.</summary>
    /// <param name="username">Restrict to one SMTP user or API key, or <see langword="null"/> for the whole account.</param>
    /// <param name="options">Per-call overrides.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<EmailSummary>> GetSummaryAsync(string? username = null, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /stats/email_cycle</c>: the billing cycle dates, usage, remaining and allowance. No request fields.</summary>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<EmailCycle>> GetCycleAsync(RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><see cref="GetCycleAsync"/> reduced to a <see cref="Quota"/>: the usual "are we about to run out" check.</summary>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<Quota> GetQuotaAsync(RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /stats/email_bounces</c>: bounces and rejects for the last 30 days.</summary>
    /// <param name="username">Restrict to one SMTP user or API key, or <see langword="null"/> for the whole account.</param>
    /// <param name="options">Per-call overrides.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<EmailBounces>> GetBouncesAsync(string? username = null, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /stats/email_spam</c>: spam complaints and rejects for the last 30 days.</summary>
    /// <param name="username">Restrict to one SMTP user or API key, or <see langword="null"/> for the whole account.</param>
    /// <param name="options">Per-call overrides.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<EmailSpam>> GetSpamAsync(string? username = null, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /stats/email_unsubs</c>: unsubscribes and rejects for the last 30 days.</summary>
    /// <param name="username">Restrict to one SMTP user or API key, or <see langword="null"/> for the whole account.</param>
    /// <param name="options">Per-call overrides.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<EmailUnsubscribes>> GetUnsubscribesAsync(string? username = null, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /stats/email_history</c>: totals per sender, user, domain or subaccount over a date range (default: last 30 days).</summary>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<EmailHistory>> GetHistoryAsync(EmailHistoryRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);
}
