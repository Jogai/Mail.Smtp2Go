using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>How SMTP2GO encodes the callback body (<c>output_format</c>). The server default is <see cref="Form"/>; this library recommends <see cref="Json"/>.</summary>
[JsonConverter(typeof(TolerantEnumConverter<WebhookOutputFormat>))]
public enum WebhookOutputFormat
{
    /// <summary>A value this library does not know; never sent to the API.</summary>
    Unknown = 0,

    /// <summary><c>application/x-www-form-urlencoded</c>; the server default.</summary>
    Form,

    /// <summary><c>application/json</c>; recommended, because arrays and header values survive intact.</summary>
    Json,
}
