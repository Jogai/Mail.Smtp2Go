using System.Runtime.CompilerServices;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="IEmailClient"/> over the shared connection.</summary>
internal sealed class EmailClient(Smtp2GoConnection connection) : IEmailClient
{
    private static readonly Endpoint s_send = EndpointTable.Get("email/send");
    private static readonly Endpoint s_mime = EndpointTable.Get("email/mime");
    private static readonly Endpoint s_batch = EndpointTable.Get("email/batch");
    private static readonly Endpoint s_scheduledSearch = EndpointTable.Get("email/scheduled/search");
    private static readonly Endpoint s_scheduledRemove = EndpointTable.Get("email/scheduled/remove");
    private static readonly Endpoint s_search = EndpointTable.Get("email/search");

    public Task<ApiResponse<EmailSendResult>> SendAsync(EmailSendRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<EmailSendRequest, EmailSendResult>(s_send, ApplyDefaults(request), options, cancellationToken);
    }

    public Task<ApiResponse<EmailSendResult>> SendMimeAsync(EmailMimeRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        EmailMimeRequest body = request.FastAccept is null && connection.Options.DefaultFastAccept is { } fastAccept ? request with { FastAccept = fastAccept } : request;
        return connection.SendAsync<EmailMimeRequest, EmailSendResult>(s_mime, body, options, cancellationToken);
    }

    public Task<ApiResponse<IReadOnlyList<EmailBatchItem>>> SendBatchAsync(EmailBatchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<EmailBatchRequest, IReadOnlyList<EmailBatchItem>>(s_batch, request, options, cancellationToken);
    }

    public Task<ApiResponse<IReadOnlyList<ScheduledEmail>>> SearchScheduledAsync(ScheduledEmailSearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<ScheduledEmailSearchRequest, IReadOnlyList<ScheduledEmail>>(s_scheduledSearch, request, options, cancellationToken);
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

#pragma warning disable CS0618 // Implements the deprecated endpoint deliberately.
    public Task<ApiResponse<EmailSearchResult>> SearchAsync(EmailSearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<EmailSearchRequest, EmailSearchResult>(s_search, request, options, cancellationToken);
    }
#pragma warning restore CS0618

    /// <summary>Fills <c>fastaccept</c> from <see cref="Smtp2GoClientOptions.DefaultFastAccept"/> when the request leaves it unset.</summary>
    private EmailSendRequest ApplyDefaults(EmailSendRequest request)
    {
        return request.FastAccept is null && connection.Options.DefaultFastAccept is { } fastAccept
            ? request with { FastAccept = fastAccept }
            : request;
    }
}
