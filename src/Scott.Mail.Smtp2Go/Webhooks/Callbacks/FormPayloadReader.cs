using System.Text.Json;

namespace Scott.Mail.Smtp2Go.Webhooks;

/// <summary>Turns form-encoded callback input into the normalised field bag: keys merged case-insensitively, repeated keys accumulated, <c>recipients</c> split on <c>,</c> and <c>;</c>.</summary>
internal static class FormPayloadReader
{
    private const string RecipientsKey = "recipients";
    private static readonly char[] s_recipientSeparators = [',', ';'];

    public static Dictionary<string, PayloadField> Read(IEnumerable<KeyValuePair<string, string?>> pairs)
    {
        Dictionary<string, (string Name, List<string> Values)> grouped = new(StringComparer.Ordinal);
        List<string> order = [];
        foreach (KeyValuePair<string, string?> pair in pairs)
        {
            if (string.IsNullOrEmpty(pair.Key))
            {
                continue;
            }

            string key = PayloadField.NormalizeKey(pair.Key);
            if (!grouped.TryGetValue(key, out (string Name, List<string> Values) entry))
            {
                entry = (pair.Key, []);
                grouped[key] = entry;
                order.Add(key);
            }

            string value = pair.Value ?? string.Empty;
            if (key == RecipientsKey)
            {
                entry.Values.AddRange(SplitRecipients(value));
            }
            else
            {
                entry.Values.Add(value);
            }
        }

        // One JSON document for the whole bag, so every field's Raw element is a proper JSON value (string or array of strings).
        using MemoryStream buffer = new();
        using (Utf8JsonWriter writer = new(buffer))
        {
            writer.WriteStartObject();
            foreach (string key in order)
            {
                (string _, List<string> values) = grouped[key];
                writer.WritePropertyName(key);
                if (values.Count == 1)
                {
                    writer.WriteStringValue(values[0]);
                }
                else
                {
                    writer.WriteStartArray();
                    foreach (string value in values)
                    {
                        writer.WriteStringValue(value);
                    }

                    writer.WriteEndArray();
                }
            }

            writer.WriteEndObject();
        }

        using JsonDocument document = JsonDocument.Parse(buffer.ToArray());
        Dictionary<string, PayloadField> fields = new(StringComparer.Ordinal);
        foreach (string key in order)
        {
            (string name, List<string> values) = grouped[key];
            fields[key] = new PayloadField(name, values.ToArray(), document.RootElement.GetProperty(key).Clone());
        }

        return fields;
    }

    /// <summary>Parses an <c>application/x-www-form-urlencoded</c> body into pairs (<c>+</c> is a space, percent-escapes decoded as UTF-8).</summary>
    public static IEnumerable<KeyValuePair<string, string?>> ParseBody(string body)
    {
        List<KeyValuePair<string, string?>> pairs = [];
        foreach (string part in body.Split('&'))
        {
            if (part.Length == 0)
            {
                continue;
            }

            int equals = part.IndexOf('=');
            string key = equals < 0 ? part : part.Substring(0, equals);
            string? value = equals < 0 ? null : part.Substring(equals + 1);
            pairs.Add(new KeyValuePair<string, string?>(Decode(key), value is null ? null : Decode(value)));
        }

        return pairs;
    }

    public static IEnumerable<string> SplitRecipients(string value)
    {
        foreach (string part in value.Split(s_recipientSeparators))
        {
            string trimmed = part.Trim();
            if (trimmed.Length > 0)
            {
                yield return trimmed;
            }
        }
    }

    private static string Decode(string text)
    {
        return Uri.UnescapeDataString(text.Replace('+', ' '));
    }
}
