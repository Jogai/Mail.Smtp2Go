namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The <c>sms/*</c> family: sending SMS messages and reporting on them. Sending needs SMS enabled on the account (a paid add-on); the reporting
/// endpoints take <c>subaccount_id</c> through <see cref="RequestOptions.SubaccountId"/>, <c>sms/send</c> does not.
/// </summary>
public interface ISmsClient
{
    /// <summary><c>POST /sms/send</c>: sends one message to up to 100 numbers. Content over 160 characters is sent as several units.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<SmsSendResult>> SendAsync(SmsSendRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /sms/summary</c>: message, unit and cost totals for a period, per subaccount.</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<SmsSummary>> GetSummaryAsync(SmsSummaryRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /sms/view-received</c>: messages received in a period (server default: the last 7 days).</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<SmsReceivedResult>> ViewReceivedAsync(SmsReceivedRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary><c>POST /sms/view-sent</c>: messages sent in a period (server default: the last 7 days).</summary>
    /// <exception cref="Smtp2GoValidationException">The request fails client-side validation; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    Task<ApiResponse<SmsSentResult>> ViewSentAsync(SmsSentRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default);
}
