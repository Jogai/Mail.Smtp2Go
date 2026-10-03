using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Scott.Mail.Smtp2Go.SpecHarvester;

/// <summary>One thing the OpenAPI validator reported about the merged specification.</summary>
public sealed record ValidationFinding
{
    /// <summary><c>error</c> or <c>warning</c>.</summary>
    public required string Severity { get; init; }

    /// <summary>The validation rule that fired, or <c>null</c> when the reader itself rejected the construct.</summary>
    public string? Rule { get; init; }

    /// <summary>JSON pointer into <c>smtp2go-v3.json</c>, for example <c>#/paths/~1email~1send/post</c>.</summary>
    public required string Location { get; init; }

    /// <summary>The validator's message.</summary>
    public required string Message { get; init; }

    /// <summary>Two findings are the same finding when severity, rule, location and message all match.</summary>
    [JsonIgnore]
    public string Key => string.Join('\u001f', Severity, Rule ?? string.Empty, Location, Message);
}

/// <summary>The content of <c>validation.json</c>: everything the validator reported for one snapshot of the merged specification.</summary>
public sealed record ValidationDocument
{
    /// <summary>The OpenAPI version the reader detected, for example <c>3.1</c>.</summary>
    public string? SpecificationVersion { get; init; }

    /// <summary>The findings, errors first, then by location and message.</summary>
    public IReadOnlyList<ValidationFinding> Findings { get; init; } = [];
}

/// <summary>
/// Validates the merged OpenAPI document with Microsoft.OpenApi before it is written. The fragments come from a third party, so a finding
/// is recorded, never fatal: the harvest always completes and the findings travel with the snapshot.
/// </summary>
public static class SpecValidator
{
    /// <summary><c>error</c>.</summary>
    public const string Error = "error";

    /// <summary><c>warning</c>.</summary>
    public const string Warning = "warning";

    /// <summary>Parses <paramref name="openApiJson"/> with the default rule set and returns what the reader and the rules reported.</summary>
    public static ValidationDocument Validate(string openApiJson)
    {
        ArgumentNullException.ThrowIfNull(openApiJson);

        List<ValidationFinding> findings = [];
        string? version = null;
        if (string.IsNullOrWhiteSpace(openApiJson))
        {
            return new ValidationDocument { Findings = [new ValidationFinding { Severity = Error, Location = "#", Message = "The document is empty." }] };
        }

        try
        {
            ReadResult result = OpenApiDocument.Parse(openApiJson, "json", new OpenApiReaderSettings { RuleSet = ValidationRuleSet.GetDefaultRuleSet(), LoadExternalRefs = false });
            if (result.Diagnostic is { } diagnostic)
            {
                findings.AddRange(diagnostic.Errors.Select(e => ToFinding(Error, e)));
                findings.AddRange(diagnostic.Warnings.Select(w => ToFinding(Warning, w)));
            }

            if (result.Document is null)
            {
                if (findings.Count == 0)
                {
                    findings.Add(new ValidationFinding { Severity = Error, Location = "#", Message = "The reader returned no document and no diagnostic." });
                }
            }
            else
            {
                // The reader reports 2.0 for anything it cannot place, so the version is only meaningful once a document exists.
                OpenApiSpecVersion detected = result.Diagnostic?.SpecificationVersion ?? default;
                version = Describe(detected);
                findings.AddRange(HarvestChecks(result.Document, detected));
            }
        }
        catch (Exception exception) when (exception is OpenApiException or System.Text.Json.JsonException or InvalidOperationException or NotSupportedException or ArgumentException)
        {
            // A document the reader cannot process at all is itself the finding.
            findings.Add(new ValidationFinding { Severity = Error, Location = "#", Message = "The document could not be read: " + exception.Message });
        }

        return new ValidationDocument
        {
            SpecificationVersion = version,
            Findings = findings
                .GroupBy(f => f.Key, StringComparer.Ordinal)
                .Select(g => g.First())
                .OrderBy(f => f.Severity, StringComparer.Ordinal)
                .ThenBy(f => f.Location, StringComparer.Ordinal)
                .ThenBy(f => f.Rule, StringComparer.Ordinal)
                .ThenBy(f => f.Message, StringComparer.Ordinal)
                .ToList(),
        };
    }

    /// <summary>The rule name of the check that the merged document is OpenAPI 3.x.</summary>
    public const string ExpectsOpenApi3Rule = "HarvestExpectsOpenApi3";

    /// <summary>The rule name of the check that the merged document describes at least one operation.</summary>
    public const string HasOperationsRule = "HarvestHasOperations";

    /// <summary>The rule name of the check that every <c>operationId</c> is unique, which the specification requires and the default rule set does not test.</summary>
    public const string UniqueOperationIdsRule = "HarvestOperationIdsAreUnique";

    /// <summary>Checks the default rule set leaves out but a merge of 69 independently published fragments can break.</summary>
    private static IEnumerable<ValidationFinding> HarvestChecks(OpenApiDocument document, OpenApiSpecVersion detected)
    {
        if (detected is not (OpenApiSpecVersion.OpenApi3_0 or OpenApiSpecVersion.OpenApi3_1 or OpenApiSpecVersion.OpenApi3_2))
        {
            yield return new ValidationFinding { Severity = Error, Rule = ExpectsOpenApi3Rule, Location = "#/openapi", Message = $"Expected an OpenAPI 3.x document but the reader detected {Describe(detected)}." };
        }

        Dictionary<string, string> seen = new(StringComparer.Ordinal);
        int operations = 0;
        if (document.Paths is not null)
        {
            foreach (var path in document.Paths.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                if (path.Value?.Operations is not { } pathOperations)
                {
                    continue;
                }

                foreach (var operation in pathOperations.OrderBy(o => o.Key.Method, StringComparer.Ordinal))
                {
                    operations++;
                    string location = "#/paths/" + Escape(path.Key) + "/" + operation.Key.Method.ToLowerInvariant();
                    string? id = operation.Value?.OperationId;
                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }

                    if (seen.TryGetValue(id, out string? first))
                    {
                        yield return new ValidationFinding { Severity = Error, Rule = UniqueOperationIdsRule, Location = location + "/operationId", Message = $"The operationId '{id}' is already used by {first}." };
                    }
                    else
                    {
                        seen.Add(id, location);
                    }
                }
            }
        }

        if (operations == 0)
        {
            yield return new ValidationFinding { Severity = Error, Rule = HasOperationsRule, Location = "#/paths", Message = "The document describes no operations." };
        }
    }

    /// <summary>Escapes one JSON pointer segment (RFC 6901).</summary>
    private static string Escape(string segment)
    {
        return segment.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
    }

    private static ValidationFinding ToFinding(string severity, OpenApiError error)
    {
        string? rule = error switch
        {
            OpenApiValidatorError validatorError => validatorError.RuleName,
            OpenApiValidatorWarning validatorWarning => validatorWarning.RuleName,
            _ => null,
        };

        return new ValidationFinding
        {
            Severity = severity,
            Rule = string.IsNullOrWhiteSpace(rule) ? null : rule,
            Location = string.IsNullOrWhiteSpace(error.Pointer) ? "#" : error.Pointer,
            Message = (error.Message ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Trim(),
        };
    }

    private static string Describe(OpenApiSpecVersion version)
    {
        return version switch
        {
            OpenApiSpecVersion.OpenApi2_0 => "2.0",
            OpenApiSpecVersion.OpenApi3_0 => "3.0",
            OpenApiSpecVersion.OpenApi3_1 => "3.1",
            OpenApiSpecVersion.OpenApi3_2 => "3.2",
            _ => version.ToString(),
        };
    }
}
