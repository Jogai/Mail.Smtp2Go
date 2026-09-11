using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go.Json.Converters;

/// <summary>
/// Reads a JSON array of strings or a single string (the <c>webhook/add</c> docs example returns <c>headers</c> and <c>usernames</c> as one string)
/// into a list, and writes an array. Apply per property with <see cref="JsonConverterAttribute"/>.
/// </summary>
public sealed class StringOrStringArrayConverter : JsonConverter<IReadOnlyList<string>>
{
    /// <inheritdoc />
    public override IReadOnlyList<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.String:
                return [reader.GetString()!];
            case JsonTokenType.StartArray:
                List<string> items = [];
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        items.Add(reader.GetString()!);
                    }
                    else if (reader.TokenType != JsonTokenType.Null)
                    {
                        throw new JsonException($"Expected a string in the array but found {reader.TokenType}.");
                    }
                }

                return items;
            default:
                throw new JsonException($"Expected a string or an array of strings but found {reader.TokenType}.");
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IReadOnlyList<string> value, JsonSerializerOptions options)
    {
        Argument.ThrowIfNull(writer);
        Argument.ThrowIfNull(value);
        writer.WriteStartArray();
        foreach (string item in value)
        {
            writer.WriteStringValue(item);
        }

        writer.WriteEndArray();
    }
}
