using System.Text.Json;

namespace Scott.Mail.Smtp2Go.Webhooks;

/// <summary>One callback field after normalisation: the first wire name seen, every value (repeated form keys and JSON arrays flatten here) and the original JSON value.</summary>
/// <param name="Name">The wire name as it first arrived (<c>Message-Id</c>, not <c>message_id</c>).</param>
/// <param name="Values">The string values; scalars are stringified, arrays flattened, objects kept as raw JSON text.</param>
/// <param name="Raw">The value as JSON, for the <see cref="WebhookEvent.Extra"/> bag.</param>
internal sealed record PayloadField(string Name, string[] Values, JsonElement Raw)
{
    /// <summary>The first non-empty value, or the first value, or <see langword="null"/> when there is none.</summary>
    public string? First
    {
        get
        {
            foreach (string value in Values)
            {
                if (value.Length > 0)
                {
                    return value;
                }
            }

            return Values.Length > 0 ? Values[0] : null;
        }
    }

    /// <summary>Lower-cases and replaces <c>-</c> with <c>_</c> so <c>Message-Id</c>, <c>message-id</c> and <c>message_id</c> share one key.</summary>
    public static string NormalizeKey(string key)
    {
        return key.Trim().ToLowerInvariant().Replace('-', '_');
    }
}
