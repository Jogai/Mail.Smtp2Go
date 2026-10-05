using System.Runtime.CompilerServices;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Generation;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="IEmailClient"/> over the shared connection.</summary>
[GeneratedClient]
internal sealed partial class EmailClient(Smtp2GoConnection connection) : IEmailClient
{
    private static readonly Endpoint s_send = EndpointTable.Get("email/send");
    private static readonly Endpoint s_mime = EndpointTable.Get("email/mime");
    private static readonly Endpoint s_scheduledRemove = EndpointTable.Get("email/scheduled/remove");

    public async Task<ApiResponse<EmailSendResult>> SendAsync(EmailSendRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        ApiResponse<EmailSendResult> response = await connection.SendAsync<EmailSendRequest, EmailSendResult>(s_send, ApplyDefaults(request), options, cancellationToken).ConfigureAwait(false);
        ReportResult(response.Data);
        return response;
    }

    public async Task<ApiResponse<EmailSendResult>> SendMimeAsync(EmailMimeRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        EmailMimeRequest body = request.FastAccept is null && connection.Options.DefaultFastAccept is { } fastAccept ? request with { FastAccept = fastAccept } : request;
        ApiResponse<EmailSendResult> response = await connection.SendAsync<EmailMimeRequest, EmailSendResult>(s_mime, body, options, cancellationToken).ConfigureAwait(false);
        ReportResult(response.Data);
        return response;
    }

    public async IAsyncEnumerable<ScheduledEmail> SearchScheduledAllAsync(ScheduledEmailSearchRequest request, RequestOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        int page = request.Page ?? 1;
        while (true)
        {
            ApiResponse<IReadOnlyList<ScheduledEmail>> response = await SearchScheduledAsync(request with { Page = page }, options, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<ScheduledEmail> items = response.Data ?? [];
            foreach (ScheduledEmail item in items)
            {
                yield return item;
            }

            if (items.Count == 0 || (request.Limit is { } limit && items.Count < limit))
            {
                yield break;
            }

            page++;
        }
    }

    public Task<ApiResponse<JsonElement>> RemoveScheduledAsync(string scheduleId, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNullOrWhiteSpace(scheduleId);
        return connection.SendAsync<ScheduledEmailRemoveRequest, JsonElement>(s_scheduledRemove, new ScheduledEmailRemoveRequest { ScheduleId = scheduleId }, options, cancellationToken);
    }

    /// <summary>
    /// Raises <see cref="ISmtp2GoDiagnostics.EmailResult"/> with the server's recipient counts. A <c>fastaccept</c> response carries neither count and
    /// raises nothing; batch items (the generated <c>SendBatchAsync</c>) carry only ids, not recipient counts, so batches raise nothing either.
    /// </summary>
    private void ReportResult(EmailSendResult? result)
    {
        if (result is { } && (result.Succeeded is not null || result.Failed is not null))
        {
            connection.Diagnostics.EmailResult(result.Succeeded ?? 0, result.Failed ?? 0);
        }
    }

    /// <summary>Fills <c>fastaccept</c> from <see cref="Smtp2GoClientOptions.DefaultFastAccept"/> when the request leaves it unset.</summary>
    private EmailSendRequest ApplyDefaults(EmailSendRequest request)
    {
        return request.FastAccept is null && connection.Options.DefaultFastAccept is { } fastAccept
            ? request with { FastAccept = fastAccept }
            : request;
    }
}
