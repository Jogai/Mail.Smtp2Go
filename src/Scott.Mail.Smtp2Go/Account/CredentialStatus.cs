using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>status</c> of a sending credential: an API key, an SMTP user or an authenticated IP. The docs list <c>allowed</c>, <c>blocked</c> and <c>sandbox</c>.</summary>
[JsonConverter(typeof(TolerantEnumConverter<CredentialStatus>))]
public enum CredentialStatus
{
    /// <summary>A value this library does not know; never sent to the API.</summary>
    Unknown = 0,

    /// <summary>Sending is allowed (the server default).</summary>
    Allowed,

    /// <summary>Sending is blocked.</summary>
    Blocked,

    /// <summary>Requests are accepted and validated but nothing is delivered.</summary>
    Sandbox,
}
