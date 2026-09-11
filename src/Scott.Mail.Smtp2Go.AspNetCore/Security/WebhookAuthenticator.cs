using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Scott.Mail.Smtp2Go.AspNetCore;

/// <summary>
/// Checks a request's <c>Authorization</c> header against the credentials configured with <c>RequireBasicAuth</c> and <c>RequireBearer</c>.
/// SMTP2GO sends the webhook's <c>auth_header_type</c>/<c>auth_header_value</c> as that header, and URL userinfo (<c>https://user:pass@host/path</c>)
/// as the same <c>Basic</c> header, so both configuration styles arrive here. Comparisons use <see cref="CryptographicOperations.FixedTimeEquals"/>.
/// </summary>
internal static class WebhookAuthenticator
{
    /// <summary>Whether any header value matches any configured credential of the same scheme.</summary>
    public static bool IsAuthorized(StringValues authorization, IReadOnlyList<WebhookCredential> credentials, IServiceProvider services)
    {
        foreach (string? value in authorization)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            string trimmed = value.Trim();
            int space = trimmed.IndexOf(' ', StringComparison.Ordinal);
            string scheme = space < 0 ? trimmed : trimmed.Substring(0, space);
            string parameter = space < 0 ? string.Empty : trimmed.Substring(space + 1).Trim();

            foreach (WebhookCredential credential in credentials)
            {
                if (string.Equals(credential.Scheme, scheme, StringComparison.OrdinalIgnoreCase) && credential.Matches(parameter, services))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Whether a <c>Basic</c> parameter (Base64 of <c>user:password</c>) carries <paramref name="username"/> and <paramref name="password"/>. The first colon
    /// separates the two, so a password may contain colons. Percent-encoded userinfo (<c>p%40ss</c> for <c>p@ss</c>) is accepted decoded as well as literal,
    /// because SMTP2GO forwards URL credentials as this header.
    /// </summary>
    public static bool MatchesBasic(string parameter, string username, string password)
    {
        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(parameter);
        }
        catch (FormatException)
        {
            return false;
        }

        string pair = Encoding.UTF8.GetString(decoded);
        int colon = pair.IndexOf(':', StringComparison.Ordinal);
        if (colon < 0)
        {
            return false;
        }

        string receivedUser = pair.Substring(0, colon);
        string receivedPassword = pair.Substring(colon + 1);

        // Both comparisons always run (no short-circuit), so a wrong user name and a wrong password take the same time.
        bool literal = FixedTimeEquals(receivedUser, username) & FixedTimeEquals(receivedPassword, password);
        bool unescaped = FixedTimeEquals(Unescape(receivedUser), username) & FixedTimeEquals(Unescape(receivedPassword), password);
        return literal | unescaped;
    }

    /// <summary>Constant-time equality of two strings compared as UTF-8. Different lengths compare unequal in constant time for the shorter length.</summary>
    public static bool FixedTimeEquals(string left, string right)
    {
        byte[] leftBytes = Encoding.UTF8.GetBytes(left);
        byte[] rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string Unescape(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value);
        }
        catch (ArgumentException)
        {
            return value;
        }
    }
}
