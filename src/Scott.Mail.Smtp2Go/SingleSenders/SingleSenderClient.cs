using Scott.Mail.Smtp2Go.Generation;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="ISingleSenderClient"/> over the shared connection.</summary>
[GeneratedClient]
internal sealed partial class SingleSenderClient(Smtp2GoConnection connection) : ISingleSenderClient
{
    private static readonly Endpoint s_remove = EndpointTable.Get("single_sender_emails/remove");

    public Task<ApiResponse<string>> RemoveAsync(string emailAddress, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNullOrWhiteSpace(emailAddress);
        return connection.SendAsync<SingleSenderRemoveRequest, string>(s_remove, new SingleSenderRemoveRequest { EmailAddress = emailAddress }, options, cancellationToken);
    }
}
