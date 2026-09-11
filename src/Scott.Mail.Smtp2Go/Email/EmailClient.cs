using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="IEmailClient"/> over the shared connection.</summary>
internal sealed class EmailClient(Smtp2GoConnection connection) : IEmailClient
{
    private static readonly Endpoint s_send = EndpointTable.Get("email/send");

    public Task<ApiResponse<EmailSendResult>> SendAsync(EmailSendRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<EmailSendRequest, EmailSendResult>(s_send, ApplyDefaults(request), options, cancellationToken);
    }

    /// <summary>Fills <c>fastaccept</c> from <see cref="Smtp2GoClientOptions.DefaultFastAccept"/> when the request leaves it unset.</summary>
    private EmailSendRequest ApplyDefaults(EmailSendRequest request)
    {
        return request.FastAccept is null && connection.Options.DefaultFastAccept is { } fastAccept
            ? request with { FastAccept = fastAccept }
            : request;
    }
}
