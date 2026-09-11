using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>Authorization</c> scheme SMTP2GO adds to callbacks (<c>auth_header_type</c>). <see cref="None"/> serialises as the documented empty string, which clears the header on edit.</summary>
[JsonConverter(typeof(TolerantEnumConverter<WebhookAuthHeaderType>))]
public enum WebhookAuthHeaderType
{
    /// <summary>A value this library does not know; never sent to the API.</summary>
    Unknown = 0,

    /// <summary>No authentication header; the documented empty string.</summary>
    [JsonStringEnumMemberName("")]
    None,

    /// <summary><c>Authorization: Basic &lt;auth_header_value&gt;</c>, where the value is <c>base64(user:pass)</c>.</summary>
    Basic,

    /// <summary><c>Authorization: Bearer &lt;auth_header_value&gt;</c>, where the value is a custom token.</summary>
    Bearer,
}
