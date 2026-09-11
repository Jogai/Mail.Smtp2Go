using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go.Json.Converters;

/// <summary>
/// Reads a string property that the API sometimes returns as a JSON number (the <c>sms/view-received</c> example shows phone numbers as
/// <c>15185550120</c>). Numbers become their invariant text, booleans <c>true</c>/<c>false</c>; strings are written back as strings. Apply per property.
/// </summary>
public sealed class NumberOrStringConverter : JsonConverter<string>
{
    /// <inheritdoc />
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out long integer))
                {
                    return integer.ToString(CultureInfo.InvariantCulture);
                }

                return reader.TryGetDecimal(out decimal number) ? number.ToString(CultureInfo.InvariantCulture) : reader.GetDouble().ToString(CultureInfo.InvariantCulture);
            case JsonTokenType.True:
                return "true";
            case JsonTokenType.False:
                return "false";
            default:
                throw new JsonException($"Expected a string or number but found {reader.TokenType}.");
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        Argument.ThrowIfNull(writer);
        writer.WriteStringValue(value);
    }
}
