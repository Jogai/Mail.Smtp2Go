using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>Event names of <c>activity/search</c> (<c>event_types</c> filter and the <c>event</c> of each result). These differ from the webhook callback names (<c>soft-bounced</c> rather than <c>bounce</c>), hence a separate enum.</summary>
[JsonConverter(typeof(TolerantEnumConverter<ActivityEventType>))]
public enum ActivityEventType
{
    /// <summary>A value this library does not know; see <see cref="ActivityEvent.EventRaw"/>. Never sent to the API.</summary>
    Unknown = 0,

    /// <summary><c>processed</c>.</summary>
    Processed,

    /// <summary><c>soft-bounced</c>.</summary>
    [JsonStringEnumMemberName("soft-bounced")]
    SoftBounced,

    /// <summary><c>hard-bounced</c>.</summary>
    [JsonStringEnumMemberName("hard-bounced")]
    HardBounced,

    /// <summary><c>rejected</c>.</summary>
    Rejected,

    /// <summary><c>spam</c>.</summary>
    Spam,

    /// <summary><c>delivered</c>.</summary>
    Delivered,

    /// <summary><c>unsubscribed</c>.</summary>
    Unsubscribed,

    /// <summary><c>resubscribed</c>.</summary>
    Resubscribed,

    /// <summary><c>opened</c>.</summary>
    Opened,

    /// <summary><c>clicked</c>.</summary>
    Clicked,
}
