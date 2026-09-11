using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>How the Allowed or Restricted Senders list is interpreted (the <c>mode</c> of <c>allowed_senders/*</c>).</summary>
[JsonConverter(typeof(TolerantEnumConverter<AllowedSendersMode>))]
public enum AllowedSendersMode
{
    /// <summary>A value this library does not know; never sent to the API.</summary>
    Unknown = 0,

    /// <summary>Only the listed addresses and domains may send (<c>whitelist</c>). Switching to this mode disables the Sender Domains and Single Sender Emails features.</summary>
    Whitelist,

    /// <summary>The listed addresses and domains may not send (<c>blacklist</c>). Switching to this mode disables the Sender Domains and Single Sender Emails features.</summary>
    Blacklist,

    /// <summary>The list is kept but not applied (<c>disabled</c>).</summary>
    Disabled,
}
