using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>How <c>stats/email_history</c> groups its rows (<c>group_by</c>). The server default is <see cref="EmailAddress"/>.</summary>
[JsonConverter(typeof(TolerantEnumConverter<EmailHistoryGroupBy>))]
public enum EmailHistoryGroupBy
{
    /// <summary>A value this library does not know; never sent to the API.</summary>
    Unknown = 0,

    /// <summary>One row per sender address (<c>email_address</c>).</summary>
    EmailAddress,

    /// <summary>One row per SMTP user or API key (<c>username</c>).</summary>
    Username,

    /// <summary>One row per sender domain (<c>domain</c>).</summary>
    Domain,

    /// <summary>One row per subaccount (<c>subaccount</c>).</summary>
    Subaccount,
}
