using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go.Json.Converters;

/// <summary>Reads and writes <see cref="BounceNotifications"/> as its wire string (<c>from</c>, <c>drop</c> or an email address).</summary>
public sealed class BounceNotificationsConverter : JsonConverter<BounceNotifications>
{
    /// <inheritdoc />
    public override BounceNotifications? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => BounceNotifications.Parse(reader.GetString() ?? string.Empty),
            _ => throw new JsonException($"Expected a bounce_notifications string but found {reader.TokenType}."),
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, BounceNotifications value, JsonSerializerOptions options)
    {
        Argument.ThrowIfNull(writer);
        Argument.ThrowIfNull(value);
        writer.WriteStringValue(value.Value);
    }
}
