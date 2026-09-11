using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// Email events a webhook can subscribe to (<c>events</c> on <c>webhook/add</c> and <c>webhook/edit</c>). The eight documented values plus
/// <see cref="Resubscribe"/>, which the Webhooks Overview lists as an event but the <c>webhook/add</c> reference omits.
/// </summary>
[JsonConverter(typeof(TolerantEnumConverter<WebhookEmailEvent>))]
public enum WebhookEmailEvent
{
    /// <summary>A value this library does not know; never sent to the API.</summary>
    Unknown = 0,

    /// <summary>The email was received by SMTP2GO and is being processed for delivery.</summary>
    Processed,

    /// <summary>The recipient server accepted the email with a 250 response.</summary>
    Delivered,

    /// <summary>The recipient opened the email.</summary>
    Open,

    /// <summary>The recipient clicked a tracked link.</summary>
    Click,

    /// <summary>The recipient server bounced the email (hard or soft).</summary>
    Bounce,

    /// <summary>The recipient reported the email as spam.</summary>
    Spam,

    /// <summary>The recipient unsubscribed through the footer link.</summary>
    Unsubscribe,

    /// <summary>The recipient resubscribed, or the address was removed from suppressions. Listed in the overview, not in the <c>webhook/add</c> reference.</summary>
    Resubscribe,

    /// <summary>The email was rejected before delivery (suppressed address, unverified sender, sandboxed credentials).</summary>
    Reject,
}
