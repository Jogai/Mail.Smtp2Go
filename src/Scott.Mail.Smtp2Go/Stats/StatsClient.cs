using Scott.Mail.Smtp2Go.Generation;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="IStatsClient"/> over the shared connection.</summary>
[GeneratedClient]
internal sealed partial class StatsClient(Smtp2GoConnection connection) : IStatsClient
{
    private static readonly Endpoint s_summary = EndpointTable.Get("stats/email_summary");
    private static readonly Endpoint s_bounces = EndpointTable.Get("stats/email_bounces");
    private static readonly Endpoint s_spam = EndpointTable.Get("stats/email_spam");
    private static readonly Endpoint s_unsubscribes = EndpointTable.Get("stats/email_unsubs");

    public Task<ApiResponse<EmailSummary>> GetSummaryAsync(string? username = null, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        return connection.SendAsync<StatsUsernameRequest, EmailSummary>(s_summary, new StatsUsernameRequest { Username = username }, options, cancellationToken);
    }

    public async Task<Quota> GetQuotaAsync(RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Quota.From(await GetCycleAsync(options, cancellationToken).ConfigureAwait(false));
    }

    public Task<ApiResponse<EmailBounces>> GetBouncesAsync(string? username = null, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        return connection.SendAsync<StatsUsernameRequest, EmailBounces>(s_bounces, new StatsUsernameRequest { Username = username }, options, cancellationToken);
    }

    public Task<ApiResponse<EmailSpam>> GetSpamAsync(string? username = null, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        return connection.SendAsync<StatsUsernameRequest, EmailSpam>(s_spam, new StatsUsernameRequest { Username = username }, options, cancellationToken);
    }

    public Task<ApiResponse<EmailUnsubscribes>> GetUnsubscribesAsync(string? username = null, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        return connection.SendAsync<StatsUsernameRequest, EmailUnsubscribes>(s_unsubscribes, new StatsUsernameRequest { Username = username }, options, cancellationToken);
    }
}
