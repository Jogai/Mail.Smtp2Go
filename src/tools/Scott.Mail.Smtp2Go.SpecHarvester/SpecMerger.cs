using System.Text.Json.Nodes;

namespace Scott.Mail.Smtp2Go.SpecHarvester;

/// <summary>Merges the per-page OpenAPI fragments into one document with deterministic key order.</summary>
public static class SpecMerger
{
    /// <summary>
    /// Unions <c>paths</c> (and the methods under a shared path), unions <c>components.securitySchemes</c>, and takes <c>openapi</c>, <c>info</c>,
    /// <c>servers</c> and <c>security</c> from the first fragment that has them. Object keys are sorted ordinally at every level.
    /// </summary>
    /// <param name="fragments">The parsed fragments, in a stable order.</param>
    /// <param name="warn">Receives a message for each conflicting operation (second definition ignored).</param>
    public static JsonObject Merge(IEnumerable<JsonObject> fragments, Action<string>? warn = null)
    {
        ArgumentNullException.ThrowIfNull(fragments);

        JsonObject merged = [];
        JsonObject paths = [];
        JsonObject securitySchemes = [];

        foreach (JsonObject fragment in fragments)
        {
            foreach (string key in new[] { "openapi", "info", "servers", "security" })
            {
                if (!merged.ContainsKey(key) && fragment[key] is JsonNode value)
                {
                    merged[key] = value.DeepClone();
                }
            }

            if (fragment["components"]?["securitySchemes"] is JsonObject schemes)
            {
                foreach (KeyValuePair<string, JsonNode?> scheme in schemes)
                {
                    if (!securitySchemes.ContainsKey(scheme.Key))
                    {
                        securitySchemes[scheme.Key] = scheme.Value?.DeepClone();
                    }
                }
            }

            if (fragment["paths"] is not JsonObject fragmentPaths)
            {
                continue;
            }

            foreach (KeyValuePair<string, JsonNode?> path in fragmentPaths)
            {
                if (path.Value is not JsonObject operations)
                {
                    continue;
                }

                if (paths[path.Key] is not JsonObject target)
                {
                    target = [];
                    paths[path.Key] = target;
                }

                foreach (KeyValuePair<string, JsonNode?> operation in operations)
                {
                    if (target.ContainsKey(operation.Key))
                    {
                        warn?.Invoke($"{operation.Key.ToUpperInvariant()} {path.Key} is defined by more than one page; keeping the first.");
                        continue;
                    }

                    target[operation.Key] = operation.Value?.DeepClone();
                }
            }
        }

        merged["components"] = new JsonObject { ["securitySchemes"] = securitySchemes };
        merged["paths"] = paths;
        return (JsonObject)SortKeys(merged);
    }

    /// <summary>Returns a copy of <paramref name="node"/> with every object's keys in ordinal order; arrays keep their order.</summary>
    public static JsonNode SortKeys(JsonNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        switch (node)
        {
            case JsonObject obj:
                JsonObject sorted = [];
                foreach (KeyValuePair<string, JsonNode?> property in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    sorted[property.Key] = property.Value is null ? null : SortKeys(property.Value);
                }

                return sorted;
            case JsonArray array:
                JsonArray copy = [];
                foreach (JsonNode? item in array)
                {
                    copy.Add(item is null ? null : SortKeys(item));
                }

                return copy;
            default:
                return node.DeepClone();
        }
    }
}
