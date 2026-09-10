using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go.Json.Converters;

/// <summary>Reads a <see cref="Percentage"/> from a JSON string (<c>"7.33"</c>) or number and writes the raw string back.</summary>
public sealed class PercentageConverter : JsonConverter<Percentage>
{
    /// <inheritdoc />
    public override Percentage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => Percentage.FromString(reader.GetString()),
            JsonTokenType.Number => Percentage.FromDecimal(reader.GetDecimal()),
            JsonTokenType.Null => default,
            _ => throw new JsonException($"Expected a percentage string or number but found {reader.TokenType}."),
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Percentage value, JsonSerializerOptions options)
    {
        Argument.ThrowIfNull(writer);
        string? text = value.Raw ?? value.Value?.ToString(CultureInfo.InvariantCulture);
        if (text is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(text);
        }
    }
}
