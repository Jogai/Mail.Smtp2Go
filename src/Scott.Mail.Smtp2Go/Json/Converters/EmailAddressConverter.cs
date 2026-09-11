using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go.Json.Converters;

/// <summary>Reads an <see cref="EmailAddress"/> from its <c>Name &lt;address&gt;</c> or bare string form and writes it back the same way.</summary>
public sealed class EmailAddressConverter : JsonConverter<EmailAddress>
{
    /// <inheritdoc />
    public override EmailAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected an email address string but found {reader.TokenType}.");
        }

        string text = reader.GetString() ?? string.Empty;
        return EmailAddress.TryParse(text, out EmailAddress address) ? address : throw new JsonException($"'{text}' is not an email address.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, EmailAddress value, JsonSerializerOptions options)
    {
        Argument.ThrowIfNull(writer);
        writer.WriteStringValue(value.ToString());
    }
}
