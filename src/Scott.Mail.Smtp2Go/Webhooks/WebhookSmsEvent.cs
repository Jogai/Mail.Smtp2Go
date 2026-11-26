using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// SMS events a webhook can subscribe to (<c>sms_events</c> on <c>webhook/add</c> and <c>webhook/edit</c>): the six documented values.
/// </summary>
[JsonConverter(typeof(TolerantEnumConverter<WebhookSmsEvent>))]
public enum WebhookSmsEvent
{
    /// <summary>A value this library does not know; never sent to the API.</summary>
    Unknown = 0,

    /// <summary>The SMS is being processed.</summary>
    Sending,

    /// <summary>The SMS was handed to the downstream provider.</summary>
    Submitted,

    /// <summary>Delivery was confirmed by the downstream provider.</summary>
    Delivered,

    /// <summary>The SMS could not be delivered.</summary>
    Failed,

    /// <summary>The SMS network blocked the message.</summary>
    Rejected,

    /// <summary>The recipient opted out of further messages. Sent as <c>opt-out</c>; <c>opt_out</c> is also read.</summary>
    [JsonStringEnumMemberName("opt-out")]
    OptOut,
}
