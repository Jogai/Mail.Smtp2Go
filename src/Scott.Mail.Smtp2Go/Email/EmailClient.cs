using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="IEmailClient"/> over the shared connection.</summary>
internal sealed class EmailClient(Smtp2GoConnection connection) : IEmailClient
{
    private static readonly Endpoint s_send = EndpointTable.Get("email/send");
    private static readonly Endpoint s_mime = EndpointTable.Get("email/mime");

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

    /// <summary>Fills <c>fastaccept</c> from <see cref="Smtp2GoClientOptions.DefaultFastAccept"/> when the request leaves it unset.</summary>
    private EmailSendRequest ApplyDefaults(EmailSendRequest request)
    {
        return request.FastAccept is null && connection.Options.DefaultFastAccept is { } fastAccept
            ? request with { FastAccept = fastAccept }
            : request;
    }
}
