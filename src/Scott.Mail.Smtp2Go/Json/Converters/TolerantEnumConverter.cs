using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go.Json.Converters;

/// <summary>
/// Reads enum values tolerantly and writes them strictly. Reading is case-insensitive, accepts the <see cref="JsonStringEnumMemberNameAttribute"/>
/// name, the member name, or either with underscores and hyphens removed, and maps anything else to the member named <c>Unknown</c>
/// (throwing <see cref="JsonException"/> when the enum has none). Writing emits the wire name and throws on <c>Unknown</c> so an
/// unrecognised value never leaves the process. Apply with <c>[JsonConverter(typeof(TolerantEnumConverter&lt;MyEnum&gt;))]</c> on the enum.
/// </summary>
/// <typeparam name="TEnum">The enum type.</typeparam>
public sealed class TolerantEnumConverter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    /// <inheritdoc />
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string text = reader.GetString() ?? string.Empty;
            if (EnumNameTable<TEnum>.TryParse(text, out TEnum parsed))
            {
                return parsed;
            }

            return EnumNameTable<TEnum>.Unknown ?? throw new JsonException($"'{text}' is not a known {typeof(TEnum).Name} value and the enum has no Unknown member.");
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out long number))
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), number);
        }

        throw new JsonException($"Expected a string for {typeof(TEnum).Name} but found {reader.TokenType}.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        Argument.ThrowIfNull(writer);
        if (EnumNameTable<TEnum>.IsUnknown(value))
        {
            throw new JsonException($"{typeof(TEnum).Name}.Unknown cannot be sent to the API.");
        }

        writer.WriteStringValue(EnumNameTable<TEnum>.GetWireName(value));
    }
}
