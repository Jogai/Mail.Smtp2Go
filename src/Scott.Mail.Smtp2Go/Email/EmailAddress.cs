using System.Text;
using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// An email address with an optional display name, serialised as the <c>Name &lt;address&gt;</c> string the API expects.
/// Parses both forms back, quotes display names that need it, and compares by address only, ignoring case.
/// A <see cref="string"/> converts implicitly, so <c>To = ["bob@example.com", "Alice &lt;alice@example.com&gt;"]</c> works.
/// </summary>
[JsonConverter(typeof(EmailAddressConverter))]
public readonly record struct EmailAddress
{
    private const string QuoteTriggers = ",<>\"\\";

    /// <summary>Creates an address with an optional display name.</summary>
    /// <param name="address">The address part, for example <c>alice@example.com</c>. Whitespace is trimmed.</param>
    /// <param name="name">The display name, or <see langword="null"/>. Whitespace is trimmed; an empty name becomes <see langword="null"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="address"/> is empty or contains whitespace or angle brackets.</exception>
    public EmailAddress(string address, string? name = null)
    {
        Argument.ThrowIfNullOrWhiteSpace(address);
        string trimmed = address.Trim();
        if (!IsPlausibleAddress(trimmed))
        {
            throw new ArgumentException($"'{address}' is not a plausible email address: it must not contain whitespace or angle brackets.", nameof(address));
        }

        Address = trimmed;
        Name = string.IsNullOrWhiteSpace(name) ? null : name!.Trim();
    }

    /// <summary>The address part, as given.</summary>
    public string Address { get; }

    /// <summary>The display name, or <see langword="null"/>.</summary>
    public string? Name { get; }

    /// <summary>Whether this is the uninitialised <see langword="default"/> value, which has no address.</summary>
    public bool IsEmpty => Address is null;

    /// <summary>Parses <c>Name &lt;address&gt;</c>, <c>"Quoted, Name" &lt;address&gt;</c>, <c>&lt;address&gt;</c> or a bare address.</summary>
    /// <exception cref="FormatException"><paramref name="text"/> is not in one of those forms.</exception>
    public static EmailAddress Parse(string text)
    {
        Argument.ThrowIfNull(text);
        return TryParse(text, out EmailAddress result) ? result : throw new FormatException($"'{text}' is not an email address. Expected 'address' or 'Name <address>'.");
    }

    /// <summary>Parses like <see cref="Parse"/> without throwing.</summary>
    public static bool TryParse(string? text, out EmailAddress result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string trimmed = text!.Trim();
        string? name = null;
        string address = trimmed;
        if (trimmed[trimmed.Length - 1] == '>')
        {
            int open = trimmed.LastIndexOf('<');
            if (open < 0)
            {
                return false;
            }

            address = trimmed.Substring(open + 1, trimmed.Length - open - 2).Trim();
            name = Unquote(trimmed.Substring(0, open).Trim());
        }

        if (!IsPlausibleAddress(address))
        {
            return false;
        }

        result = new EmailAddress(address, name);
        return true;
    }

    /// <summary>Parses a string; equivalent to <see cref="Parse"/>.</summary>
    public static implicit operator EmailAddress(string text)
    {
        return Parse(text);
    }

    /// <summary>Named alternative to the implicit conversion from <see cref="string"/>.</summary>
    public static EmailAddress FromString(string text)
    {
        return Parse(text);
    }

    /// <summary>Two addresses are equal when their address parts match ignoring case; the display name does not take part.</summary>
    public bool Equals(EmailAddress other)
    {
        return string.Equals(Address, other.Address, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Address is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Address);
    }

    /// <summary>The wire form: the bare address, or <c>Name &lt;address&gt;</c> with the name quoted when it contains <c>,</c>, <c>&lt;</c>, <c>&gt;</c>, <c>"</c> or <c>\</c>.</summary>
    public override string ToString()
    {
        if (Address is null)
        {
            return string.Empty;
        }

        if (Name is null)
        {
            return Address;
        }

        return Quote(Name) + " <" + Address + ">";
    }

    private static bool IsPlausibleAddress(string address)
    {
        if (address.Length == 0)
        {
            return false;
        }

        foreach (char c in address)
        {
            if (char.IsWhiteSpace(c) || c == '<' || c == '>')
            {
                return false;
            }
        }

        return true;
    }

    private static string Quote(string name)
    {
        if (!NeedsQuoting(name))
        {
            return name;
        }

        StringBuilder builder = new(name.Length + 2);
        builder.Append('"');
        foreach (char c in name)
        {
            if (c == '"' || c == '\\')
            {
                builder.Append('\\');
            }

            builder.Append(c);
        }

        builder.Append('"');
        return builder.ToString();
    }

    private static bool NeedsQuoting(string name)
    {
        foreach (char c in name)
        {
            if (QuoteTriggers.Contains(c))
            {
                return true;
            }
        }

        return false;
    }

    private static string? Unquote(string name)
    {
        if (name.Length >= 2 && name[0] == '"' && name[name.Length - 1] == '"')
        {
            StringBuilder builder = new(name.Length);
            for (int i = 1; i < name.Length - 1; i++)
            {
                char c = name[i];
                if (c == '\\' && i + 1 < name.Length - 1)
                {
                    c = name[++i];
                }

                builder.Append(c);
            }

            name = builder.ToString();
        }

        return name.Length == 0 ? null : name;
    }
}
