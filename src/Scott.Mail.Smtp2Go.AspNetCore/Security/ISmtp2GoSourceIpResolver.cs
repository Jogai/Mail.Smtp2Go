using System.Net;

namespace Scott.Mail.Smtp2Go.AspNetCore;

/// <summary>
/// Supplies the addresses SMTP2GO delivers callbacks from, for <c>RequireSmtp2GoSourceIp()</c>. The default, <see cref="Smtp2GoSourceIpResolver"/>,
/// resolves <c>webhooks.smtp2go.com</c>; register another implementation (a fixed list, a stub in tests) before <c>AddSmtp2GoWebhooks</c> to replace it.
/// </summary>
public interface ISmtp2GoSourceIpResolver
{
    /// <summary>The addresses a callback may come from. IPv4-mapped IPv6 addresses are returned as IPv4.</summary>
    /// <exception cref="Exception">The addresses could not be determined; the endpoint then answers 503 so SMTP2GO retries.</exception>
    ValueTask<IReadOnlyCollection<IPAddress>> GetAddressesAsync(CancellationToken cancellationToken);
}
