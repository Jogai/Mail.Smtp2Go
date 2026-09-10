using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go.Json.Converters;

/// <summary>
/// Reads every timestamp format the SMTP2GO API has been observed to return (<c>2022-11-01 00:00:00+00:00</c>, <c>2016-08-04 01:49:15.863998</c>,
/// <c>2025-09-10 13:15:00 +1200</c>, ISO-8601 with <c>Z</c> or an offset, and date-only) and writes ISO-8601 UTC (<c>yyyy-MM-ddTHH:mm:ssZ</c>).
/// Values without an offset are taken as UTC.
/// </summary>
public sealed class Smtp2GoDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    private const string WriteFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    private static readonly string[] s_formats =
    [
        "yyyy-MM-dd HH:mm:sszzz",
        "yyyy-MM-dd HH:mm:ss.FFFFFFFzzz",
        "yyyy-MM-dd HH:mm:ss zzz",
        "yyyy-MM-dd HH:mm:ss.FFFFFFF zzz",
        "yyyy-MM-dd HH:mm:ss'Z'",
        "yyyy-MM-dd HH:mm:ss.FFFFFFF'Z'",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss.FFFFFFF",
        "yyyy-MM-dd'T'HH:mm:sszzz",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz",
        "yyyy-MM-dd'T'HH:mm:ss'Z'",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
        "yyyy-MM-dd'T'HH:mm:ss",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF",
        "yyyy-MM-dd",
    ];

    /// <inheritdoc />
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected a timestamp string but found {reader.TokenType}.");
        }

        string text = reader.GetString() ?? string.Empty;
        return TryParse(text, out DateTimeOffset value) ? value : throw new JsonException($"'{text}' is not a recognised SMTP2GO timestamp.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        Argument.ThrowIfNull(writer);
        writer.WriteStringValue(Format(value));
    }

    /// <summary>Formats <paramref name="value"/> the way the converter writes it: ISO-8601 in UTC, whole seconds.</summary>
    public static string Format(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString(WriteFormat, CultureInfo.InvariantCulture);
    }

    /// <summary>Parses any of the observed timestamp formats. Values without an offset are taken as UTC.</summary>
    public static bool TryParse(string text, out DateTimeOffset value)
    {
        Argument.ThrowIfNull(text);
        string trimmed = InsertOffsetColon(text.Trim());
        const DateTimeStyles styles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowInnerWhite;
        return DateTimeOffset.TryParseExact(trimmed, s_formats, CultureInfo.InvariantCulture, styles, out value)
            || DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, styles, out value);
    }

    /// <summary>Turns a trailing compact offset such as <c>+1200</c> into <c>+12:00</c>, which the <c>zzz</c> specifier parses.</summary>
    private static string InsertOffsetColon(string text)
    {
        if (text.Length < 5)
        {
            return text;
        }

        int signIndex = text.Length - 5;
        char sign = text[signIndex];
        if ((sign != '+' && sign != '-') || !AllDigits(text, signIndex + 1))
        {
            return text;
        }

        return text.Insert(text.Length - 2, ":");
    }

    private static bool AllDigits(string text, int start)
    {
        for (int i = start; i < text.Length; i++)
        {
            if (text[i] < '0' || text[i] > '9')
            {
                return false;
            }
        }

        return true;
    }
}
