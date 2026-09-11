using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// SMS events a webhook can subscribe to (<c>sms_events</c> on <c>webhook/add</c> and <c>webhook/edit</c>). The five documented values plus
/// <see cref="OptOut"/>, which the Webhooks Overview lists as an SMS event but the <c>webhook/add</c> reference omits.
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

    /// <summary>The recipient opted out of further messages. Listed in the overview, not in the <c>webhook/add</c> reference.</summary>
    [JsonStringEnumMemberName("opt_out")]
    OptOut,
}
