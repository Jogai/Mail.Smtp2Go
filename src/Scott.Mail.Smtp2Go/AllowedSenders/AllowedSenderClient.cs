using Scott.Mail.Smtp2Go.Generation;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="IAllowedSenderClient"/> over the shared connection.</summary>
[GeneratedClient]
internal sealed partial class AllowedSenderClient(Smtp2GoConnection connection) : IAllowedSenderClient
{
}
