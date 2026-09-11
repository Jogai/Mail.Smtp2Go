namespace Scott.Mail.Smtp2Go;

/// <summary>What a <see cref="BounceNotifications"/> value asks the server to do with bounce notifications.</summary>
public enum BounceNotificationsKind
{
    /// <summary>Return the bounce to the sender (<c>from</c>, the server default).</summary>
    From = 0,

    /// <summary>Discard the bounce (<c>drop</c>).</summary>
    Drop,

    /// <summary>Send the bounce to the address in <see cref="BounceNotifications.EmailAddress"/>.</summary>
    Email,
}
