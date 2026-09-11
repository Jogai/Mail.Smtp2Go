using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Scott.Mail.Smtp2Go.Json.Converters;

/// <summary>
/// Reads the <c>data</c> of <c>webhook/view</c>, which the live API returns as an array while the docs example shows a single object.
/// Either shape becomes a list; a single object becomes a one-item list. Registered in the serializer context for <see cref="IReadOnlyList{T}"/> of <see cref="Webhook"/>.
/// </summary>
public sealed class WebhookListConverter : JsonConverter<IReadOnlyList<Webhook>>
{
    /// <inheritdoc />
    public override IReadOnlyList<Webhook>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Argument.ThrowIfNull(options);
        JsonTypeInfo<Webhook> itemInfo = (JsonTypeInfo<Webhook>)options.GetTypeInfo(typeof(Webhook));
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.StartObject:
                return [JsonSerializer.Deserialize(ref reader, itemInfo)!];
            case JsonTokenType.StartArray:
                List<Webhook> items = [];
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        items.Add(JsonSerializer.Deserialize(ref reader, itemInfo)!);
                    }
                    else if (reader.TokenType != JsonTokenType.Null)
                    {
                        throw new JsonException($"Expected a webhook object in the array but found {reader.TokenType}.");
                    }
                }

                return items;
            default:
                throw new JsonException($"Expected a webhook object or an array of webhooks but found {reader.TokenType}.");
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IReadOnlyList<Webhook> value, JsonSerializerOptions options)
    {
        Argument.ThrowIfNull(writer);
        Argument.ThrowIfNull(value);
        Argument.ThrowIfNull(options);
        JsonTypeInfo<Webhook> itemInfo = (JsonTypeInfo<Webhook>)options.GetTypeInfo(typeof(Webhook));
        writer.WriteStartArray();
        foreach (Webhook item in value)
        {
            JsonSerializer.Serialize(writer, item, itemInfo);
        }

        writer.WriteEndArray();
    }
}
