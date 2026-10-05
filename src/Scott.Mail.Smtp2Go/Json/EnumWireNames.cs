using Scott.Mail.Smtp2Go.Generation;

namespace Scott.Mail.Smtp2Go.Json;

/// <summary>
/// Build-time tables of the library's enums that are read with <see cref="Converters.TolerantEnumConverter{TEnum}"/>: the generated part adds
/// <c>Get&lt;TEnum&gt;()</c>, which returns the members and wire names for those enums and null for any other, so
/// <see cref="EnumNameTable{TEnum}"/> reflects over an enum only when it comes from another assembly.
/// </summary>
[GeneratedEnumWireNames]
internal static partial class EnumWireNames
{
}
