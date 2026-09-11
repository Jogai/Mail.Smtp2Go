using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Scott.Mail.Smtp2Go.Json.Converters;

/// <summary>
/// Reads a list that the API returns either as a bare array (the <c>PATCH</c> edit endpoints) or wrapped as <c>{"results": [...]}</c> (the
/// <c>POST</c> add, edit and remove endpoints of SMTP users), and writes an array. Wrapper fields other than <c>results</c> are skipped; a wrapper
/// without <c>results</c> reads as an empty list. Registered in the serializer context per item type.
/// </summary>
/// <typeparam name="TItem">The item type, which must be registered in the serializer context.</typeparam>
public sealed class ResultsListConverter<TItem> : JsonConverter<IReadOnlyList<TItem>>
{
    private const string ResultsProperty = "results";

    /// <inheritdoc />
    public override IReadOnlyList<TItem>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Argument.ThrowIfNull(options);
        JsonTypeInfo<TItem> itemInfo = (JsonTypeInfo<TItem>)options.GetTypeInfo(typeof(TItem));
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.StartArray:
                return ReadArray(ref reader, itemInfo);
            case JsonTokenType.StartObject:
                List<TItem> results = [];
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    bool isResults = reader.ValueTextEquals(ResultsProperty);
                    reader.Read();
                    if (isResults && reader.TokenType == JsonTokenType.StartArray)
                    {
                        results = ReadArray(ref reader, itemInfo);
                    }
                    else
                    {
                        reader.Skip();
                    }
                }

                return results;
            default:
                throw new JsonException($"Expected an array of {typeof(TItem).Name} or a results wrapper but found {reader.TokenType}.");
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IReadOnlyList<TItem> value, JsonSerializerOptions options)
    {
        Argument.ThrowIfNull(writer);
        Argument.ThrowIfNull(value);
        Argument.ThrowIfNull(options);
        JsonTypeInfo<TItem> itemInfo = (JsonTypeInfo<TItem>)options.GetTypeInfo(typeof(TItem));
        writer.WriteStartArray();
        foreach (TItem item in value)
        {
            JsonSerializer.Serialize(writer, item, itemInfo);
        }

        writer.WriteEndArray();
    }

    private static List<TItem> ReadArray(ref Utf8JsonReader reader, JsonTypeInfo<TItem> itemInfo)
    {
        List<TItem> items = [];
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                continue;
            }

            items.Add(JsonSerializer.Deserialize(ref reader, itemInfo)!);
        }

        return items;
    }
}
