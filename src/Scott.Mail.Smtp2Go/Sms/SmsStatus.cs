using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>The per-message <c>status</c> of <c>sms/send</c>, as the docs enumerate it.</summary>
[JsonConverter(typeof(TolerantEnumConverter<SmsStatus>))]
public enum SmsStatus
{
    /// <summary>A value this library does not know.</summary>
    Unknown = 0,

    /// <summary><c>processing</c>.</summary>
    Processing,

    /// <summary><c>enroute</c>.</summary>
    Enroute,

    /// <summary><c>queued</c>.</summary>
    Queued,

    /// <summary><c>submitted</c>.</summary>
    Submitted,

    /// <summary><c>processed</c>.</summary>
    Processed,

    /// <summary><c>delivered</c>.</summary>
    Delivered,

    /// <summary><c>held</c>.</summary>
    Held,

    /// <summary><c>expired</c>.</summary>
    Expired,

    /// <summary><c>cancelled</c>.</summary>
    Cancelled,

    /// <summary><c>failed</c>.</summary>
    Failed,

    /// <summary><c>rejected</c>.</summary>
    Rejected,
}
