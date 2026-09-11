using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="ISmsClient"/> over the shared connection.</summary>
internal sealed class SmsClient(Smtp2GoConnection connection) : ISmsClient
{
    private static readonly Endpoint s_send = EndpointTable.Get("sms/send");
    private static readonly Endpoint s_summary = EndpointTable.Get("sms/summary");
    private static readonly Endpoint s_received = EndpointTable.Get("sms/view-received");
    private static readonly Endpoint s_sent = EndpointTable.Get("sms/view-sent");

    public Task<ApiResponse<SmsSendResult>> SendAsync(SmsSendRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<SmsSendRequest, SmsSendResult>(s_send, request, options, cancellationToken);
    }

    public Task<ApiResponse<SmsSummary>> GetSummaryAsync(SmsSummaryRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<SmsSummaryRequest, SmsSummary>(s_summary, request, options, cancellationToken);
    }

    public Task<ApiResponse<SmsReceivedResult>> ViewReceivedAsync(SmsReceivedRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<SmsReceivedRequest, SmsReceivedResult>(s_received, request, options, cancellationToken);
    }

    public Task<ApiResponse<SmsSentResult>> ViewSentAsync(SmsSentRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<SmsSentRequest, SmsSentResult>(s_sent, request, options, cancellationToken);
    }
}
