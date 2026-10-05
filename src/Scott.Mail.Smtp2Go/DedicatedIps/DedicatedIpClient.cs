using Scott.Mail.Smtp2Go.Generation;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="IDedicatedIpClient"/> over the shared connection.</summary>
[GeneratedClient]
internal sealed partial class DedicatedIpClient(Smtp2GoConnection connection) : IDedicatedIpClient
{
}
