using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Scott.Mail.Smtp2Go.SpecHarvester;

/// <summary>Turns one parsed page fragment into <see cref="OperationSummary"/> rows for <c>endpoints.json</c>.</summary>
public static partial class OperationSummarizer
{
    /// <summary>The wire name whose presence sets <see cref="OperationSummary.AcceptsSubaccountId"/>.</summary>
    public const string SubaccountIdProperty = "subaccount_id";

    /// <summary>Summarises every operation in <paramref name="page"/>; an unparsable fragment yields one <c>unparsed</c> row when its path can be guessed.</summary>
    public static IReadOnlyList<OperationSummary> Summarize(PageFragment page)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (page.Spec is null)
        {
            if (page.RawFragment is null)
            {
                return [];
            }

            string? guessed = FragmentExtractor.GuessPath(page.RawFragment);
            return guessed is null
                ? []
                : [new OperationSummary
                {
                    Path = guessed,
                    Method = "POST",
                    Page = page.Slug,
                    DocsUpdatedAt = page.UpdatedAt,
                    Spec = "unparsed",
                    Description = page.Prose,
                    Error = page.Error,
                }];
        }

        List<OperationSummary> rows = [];
        if (page.Spec["paths"] is not JsonObject paths)
        {
            return rows;
        }

        foreach (KeyValuePair<string, JsonNode?> path in paths)
        {
            if (path.Value is not JsonObject operations)
            {
                continue;
            }

            foreach (KeyValuePair<string, JsonNode?> operation in operations)
            {
                if (operation.Value is JsonObject op)
                {
                    rows.Add(Summarize(page, path.Key.TrimStart('/'), operation.Key.ToUpperInvariant(), op));
                }
            }
        }

        return rows;
    }

    /// <summary>The <c>info.version</c> of a fragment, if present.</summary>
    public static string? ApiVersion(JsonObject spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        return spec["info"]?["version"]?.GetValue<string>();
    }

    /// <summary>Parses "rate-limited to N requests per minute" (and the "rate limited to N calls per hour" variant) out of a description.</summary>
    public static RateLimitNote? ParseRateLimit(string? description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return null;
        }

        Match match = RateLimit().Match(description);
        return match.Success
            ? new RateLimitNote
            {
                Limit = int.Parse(match.Groups["limit"].Value, CultureInfo.InvariantCulture),
                Per = match.Groups["per"].Value.ToLowerInvariant(),
                Text = match.Value,
            }
            : null;
    }

    private static OperationSummary Summarize(PageFragment page, string path, string method, JsonObject op)
    {
        JsonObject? requestSchema = op["requestBody"]?["content"]?["application/json"]?["schema"] as JsonObject;
        List<PropertySummary> requestProperties = Properties(requestSchema?["properties"] as JsonObject);
        List<string> required = requestSchema?["required"] is JsonArray requiredArray
            ? requiredArray.Select(n => n?.GetValue<string>()).Where(s => s is not null).Select(s => s!).ToList()
            : [];

        JsonObject? responseContent = op["responses"]?["200"]?["content"]?["application/json"] as JsonObject;
        JsonObject? dataSchema = ResponseDataSchema(responseContent?["schema"] as JsonObject);
        string? shape = dataSchema?["type"]?.GetValue<string>();
        JsonObject? dataProperties = dataSchema?["properties"] as JsonObject
            ?? (string.Equals(shape, "array", StringComparison.Ordinal) ? dataSchema?["items"]?["properties"] as JsonObject : null);

        string? description = op["description"]?.GetValue<string>();

        return new OperationSummary
        {
            Path = path,
            Method = method,
            OperationId = op["operationId"]?.GetValue<string>(),
            Page = page.Slug,
            Summary = op["summary"]?.GetValue<string>(),
            DocsUpdatedAt = page.UpdatedAt,
            Spec = "parsed",
            Deprecated = op["deprecated"]?.GetValue<bool>() ?? false,
            Required = required,
            RequestProperties = requestProperties,
            AcceptsSubaccountId = requestProperties.Any(p => string.Equals(p.Name, SubaccountIdProperty, StringComparison.Ordinal)),
            RateLimitNote = ParseRateLimit(description),
            ResponseShape = shape,
            ResponseProperties = Properties(dataProperties),
            ResponseExample = ResponseExample(responseContent),
        };
    }

    /// <summary>
    /// The schema of the response <c>data</c>. Most pages document the envelope (<c>request_id</c> plus <c>data</c>); a few document the
    /// <c>data</c> array or object directly, which is recognised by the absence of a <c>data</c> property.
    /// </summary>
    private static JsonObject? ResponseDataSchema(JsonObject? schema)
    {
        if (schema is null)
        {
            return null;
        }

        if (schema["properties"]?["data"] is JsonObject data)
        {
            return data;
        }

        return schema["properties"]?["request_id"] is null ? schema : null;
    }

    private static JsonNode? ResponseExample(JsonObject? content)
    {
        if (content is null)
        {
            return null;
        }

        if (content["examples"] is JsonObject examples)
        {
            foreach (KeyValuePair<string, JsonNode?> example in examples)
            {
                if (example.Value?["value"] is JsonNode value)
                {
                    return value.DeepClone();
                }
            }
        }

        return content["example"]?.DeepClone();
    }

    private static List<PropertySummary> Properties(JsonObject? properties)
    {
        List<PropertySummary> list = [];
        if (properties is null)
        {
            return list;
        }

        foreach (KeyValuePair<string, JsonNode?> property in properties)
        {
            JsonObject? schema = property.Value as JsonObject;
            list.Add(new PropertySummary
            {
                Name = property.Key,
                Type = schema?["type"] is JsonValue type ? type.ToString() : null,
                Deprecated = schema?["deprecated"]?.GetValue<bool>() ?? false,
            });
        }

        return list;
    }

    [GeneratedRegex(@"rate[\s-]*limited to (?<limit>\d+) (?:requests|calls) per (?<per>minute|hour|second|day)", RegexOptions.IgnoreCase)]
    private static partial Regex RateLimit();
}
