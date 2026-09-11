using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>state</c> of a subaccount. The API returns it capitalised (<c>Active</c>); parsing is case-insensitive.</summary>
[JsonConverter(typeof(TolerantEnumConverter<SubaccountState>))]
public enum SubaccountState
{
    /// <summary>A value this library does not know; see <see cref="Subaccount.StateRaw"/>.</summary>
    Unknown = 0,

    /// <summary>The subaccount can send.</summary>
    Active,

    /// <summary>The subaccount was closed (<c>subaccount/close</c>) and can be reopened.</summary>
    Closed,

    /// <summary>The subaccount was suspended by SMTP2GO.</summary>
    Suspended,
}
