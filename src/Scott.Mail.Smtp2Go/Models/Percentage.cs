using System.Globalization;
using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>A percentage the API returns as a string such as <c>"7.33"</c>, exposed as a <see cref="decimal"/> with the raw text retained.</summary>
/// <param name="Value">The parsed value, or <see langword="null"/> when <paramref name="Raw"/> is not a number.</param>
/// <param name="Raw">Exactly what arrived on the wire.</param>
[JsonConverter(typeof(PercentageConverter))]
public readonly record struct Percentage(decimal? Value, string? Raw)
{
    /// <summary>Creates a percentage from a raw string, parsing it with the invariant culture.</summary>
    public static Percentage FromString(string? raw)
    {
        return raw is not null && decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value)
            ? new Percentage(value, raw)
            : new Percentage(null, raw);
    }

    /// <summary>Creates a percentage from a value; <see cref="Raw"/> is the invariant-culture text.</summary>
    public static Percentage FromDecimal(decimal value)
    {
        return new Percentage(value, value.ToString(CultureInfo.InvariantCulture));
    }
}
