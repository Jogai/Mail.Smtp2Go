using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The <c>bounce_notifications</c> setting of an API key, SMTP user or authenticated IP: on the wire it is the string <c>from</c> (return the bounce
/// to the sender), <c>drop</c> (discard it) or an email address (forward it there). Use <see cref="From"/>, <see cref="Drop"/> or <see cref="Email"/>.
/// </summary>
[JsonConverter(typeof(BounceNotificationsConverter))]
public sealed record BounceNotifications
{
    private const string FromValue = "from";
    private const string DropValue = "drop";

    private BounceNotifications(BounceNotificationsKind kind, string value)
    {
        Kind = kind;
        Value = value;
    }

    /// <summary>Return bounces to the sender (<c>from</c>), the server default.</summary>
    public static BounceNotifications From { get; } = new(BounceNotificationsKind.From, FromValue);

    /// <summary>Discard bounces (<c>drop</c>).</summary>
    public static BounceNotifications Drop { get; } = new(BounceNotificationsKind.Drop, DropValue);

    /// <summary>What the value asks for.</summary>
    public BounceNotificationsKind Kind { get; }

    /// <summary>The wire value: <c>from</c>, <c>drop</c> or the address.</summary>
    public string Value { get; }

    /// <summary>The forwarding address when <see cref="Kind"/> is <see cref="BounceNotificationsKind.Email"/>; otherwise <see langword="null"/>.</summary>
    public string? EmailAddress => Kind == BounceNotificationsKind.Email ? Value : null;

    /// <summary>Forward bounces to <paramref name="address"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="address"/> is blank or has no <c>@</c>.</exception>
    public static BounceNotifications Email(string address)
    {
        Argument.ThrowIfNullOrWhiteSpace(address);
        string trimmed = address.Trim();
        if (trimmed.IndexOf('@') <= 0)
        {
            throw new ArgumentException("bounce_notifications must be 'from', 'drop' or an email address.", nameof(address));
        }

        return new BounceNotifications(BounceNotificationsKind.Email, trimmed);
    }

    /// <summary>Reads a wire value: <c>from</c> and <c>drop</c> (any case) map to the singletons, anything else is taken as a forwarding address without validation.</summary>
    public static BounceNotifications Parse(string value)
    {
        Argument.ThrowIfNull(value);
        string trimmed = value.Trim();
        if (string.Equals(trimmed, FromValue, StringComparison.OrdinalIgnoreCase))
        {
            return From;
        }

        if (string.Equals(trimmed, DropValue, StringComparison.OrdinalIgnoreCase))
        {
            return Drop;
        }

        return new BounceNotifications(BounceNotificationsKind.Email, trimmed);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value;
    }
}
