using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>states</c> filter of <c>subaccounts/search</c>: one state, or <see cref="All"/> (the server default).</summary>
[JsonConverter(typeof(TolerantEnumConverter<SubaccountStateFilter>))]
public enum SubaccountStateFilter
{
    /// <summary>A value this library does not know; never sent to the API.</summary>
    Unknown = 0,

    /// <summary>Every state (<c>all</c>).</summary>
    All,

    /// <summary>Only active subaccounts.</summary>
    Active,

    /// <summary>Only closed subaccounts.</summary>
    Closed,

    /// <summary>Only suspended subaccounts.</summary>
    Suspended,
}
