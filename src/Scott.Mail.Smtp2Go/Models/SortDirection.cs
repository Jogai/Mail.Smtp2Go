using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>Sort order for list endpoints (<c>sort_direction</c> on <c>template/search</c>, <c>sort</c> on <c>suppression/view</c>).</summary>
[JsonConverter(typeof(TolerantEnumConverter<SortDirection>))]
public enum SortDirection
{
    /// <summary>A value this library does not know; never sent to the API.</summary>
    Unknown = 0,

    /// <summary>Ascending (<c>asc</c>), the server default.</summary>
    Asc,

    /// <summary>Descending (<c>desc</c>).</summary>
    Desc,
}
