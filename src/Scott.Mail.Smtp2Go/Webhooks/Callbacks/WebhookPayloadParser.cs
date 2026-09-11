using System.Text.Json;

namespace Scott.Mail.Smtp2Go.Webhooks;

/// <summary>
/// Parses SMTP2GO callback bodies, JSON (<c>output_format: json</c>) or form-encoded (the server default), into the <see cref="WebhookEvent"/> subtype for the
/// <c>event</c> field. Key case and <c>-</c>/<c>_</c> differences are normalised (<c>Message-Id</c>, <c>message-id</c>), both documented and live event names are
/// accepted (<c>open</c> and <c>opened</c>), repeated form keys accumulate, and <c>recipients</c> is split on <c>,</c> and <c>;</c>. Thread-safe; reuse one instance.
/// </summary>
public sealed class WebhookPayloadParser
{
    private static readonly JsonDocumentOptions s_documentOptions = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    /// <summary>Creates a parser.</summary>
    /// <param name="options">Parser settings; <see langword="null"/> for the defaults (no declared custom headers).</param>
    public WebhookPayloadParser(WebhookParserOptions? options = null)
    {
        Options = options ?? new WebhookParserOptions();
    }

    /// <summary>A parser with default options.</summary>
    public static WebhookPayloadParser Default { get; } = new();

    /// <summary>The settings in effect.</summary>
    public WebhookParserOptions Options { get; }

    /// <summary>Parses a JSON callback body.</summary>
    /// <exception cref="JsonException">The body is not a JSON object.</exception>
    public WebhookEvent Parse(ReadOnlySpan<byte> utf8Json)
    {
        using JsonDocument document = JsonDocument.Parse(utf8Json.ToArray(), s_documentOptions);
        return FromDocument(document);
    }

    /// <summary>Parses a JSON callback body.</summary>
    /// <exception cref="JsonException">The body is not a JSON object.</exception>
    public WebhookEvent Parse(string json)
    {
        Argument.ThrowIfNull(json);
        using JsonDocument document = JsonDocument.Parse(json, s_documentOptions);
        return FromDocument(document);
    }

    /// <summary>Parses a JSON callback body from a stream (for example an HTTP request body).</summary>
    /// <exception cref="JsonException">The body is not a JSON object.</exception>
    public async Task<WebhookEvent> ParseAsync(Stream utf8Json, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(utf8Json);
        using JsonDocument document = await JsonDocument.ParseAsync(utf8Json, s_documentOptions, cancellationToken).ConfigureAwait(false);
        return FromDocument(document);
    }

    /// <summary>Parses form-encoded callback input already split into pairs (for example ASP.NET Core's <c>IFormCollection</c>). Keys may repeat.</summary>
    public WebhookEvent ParseForm(IEnumerable<KeyValuePair<string, string?>> pairs)
    {
        Argument.ThrowIfNull(pairs);
        return WebhookEventFactory.Create(FormPayloadReader.Read(pairs), Options);
    }

    /// <summary>Parses a raw <c>application/x-www-form-urlencoded</c> body.</summary>
    public WebhookEvent ParseFormBody(string body)
    {
        Argument.ThrowIfNull(body);
        return ParseForm(FormPayloadReader.ParseBody(body));
    }

    /// <summary>Parses a body by its media type: <c>application/json</c> as JSON, <c>application/x-www-form-urlencoded</c> as a form; anything else is tried as JSON when it starts with <c>{</c>, otherwise as a form.</summary>
    /// <param name="body">The request body.</param>
    /// <param name="contentType">The <c>Content-Type</c> header value, or <see langword="null"/> to sniff.</param>
    public WebhookEvent Parse(string body, string? contentType)
    {
        Argument.ThrowIfNull(body);
        string mediaType = contentType is null ? string.Empty : contentType.Split(';')[0].Trim();
        if (mediaType.EndsWith("json", StringComparison.OrdinalIgnoreCase))
        {
            return Parse(body);
        }

        if (string.Equals(mediaType, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
        {
            return ParseFormBody(body);
        }

        string trimmed = body.TrimStart();
        return trimmed.Length > 0 && trimmed[0] == '{' ? Parse(body) : ParseFormBody(body);
    }

    private WebhookEvent FromDocument(JsonDocument document)
    {
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"A webhook callback must be a JSON object but the body is {root.ValueKind}.");
        }

        Dictionary<string, PayloadField> fields = new(StringComparer.Ordinal);
        foreach (JsonProperty property in root.EnumerateObject())
        {
            string key = PayloadField.NormalizeKey(property.Name);
            string[] values = Flatten(property.Value);
            if (fields.TryGetValue(key, out PayloadField? existing))
            {
                // Live form payloads carry both "Message-Id" and "message-id"; JSON may too. Keep the first name, accumulate values.
                fields[key] = existing with { Values = Merge(existing.Values, values) };
            }
            else
            {
                fields[key] = new PayloadField(property.Name, values, property.Value.Clone());
            }
        }

        return WebhookEventFactory.Create(fields, Options);
    }

    private static string[] Merge(string[] first, string[] second)
    {
        if (second.Length == 0)
        {
            return first;
        }

        List<string> merged = new(first);
        foreach (string value in second)
        {
            if (!merged.Contains(value))
            {
                merged.Add(value);
            }
        }

        return merged.ToArray();
    }

    private static string[] Flatten(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return [];
            case JsonValueKind.String:
                return [value.GetString() ?? string.Empty];
            case JsonValueKind.Number:
                return [value.GetRawText()];
            case JsonValueKind.True:
                return [bool.TrueString.ToLowerInvariant()];
            case JsonValueKind.False:
                return [bool.FalseString.ToLowerInvariant()];
            case JsonValueKind.Array:
                List<string> items = [];
                foreach (JsonElement item in value.EnumerateArray())
                {
                    items.AddRange(Flatten(item));
                }

                return items.ToArray();
            default:
                return [value.GetRawText()];
        }
    }
}
