namespace Scott.Mail.Smtp2Go.Webhooks;

/// <summary>How SMTP2GO classified a bounce (the <c>bounce</c> callback field).</summary>
public enum BounceType
{
    /// <summary>A value this library does not know; see <see cref="EmailBounceEvent.BounceRaw"/>.</summary>
    Unknown = 0,

    /// <summary>Permanent failure; the address is suppressed.</summary>
    Hard,

    /// <summary>Temporary failure (mailbox full, greylisting, ...).</summary>
    Soft,
}
