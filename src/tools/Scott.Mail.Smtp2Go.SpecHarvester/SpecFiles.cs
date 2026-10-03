using System.Text.Json;
using System.Text.Json.Nodes;

namespace Scott.Mail.Smtp2Go.SpecHarvester;

/// <summary>File names under <c>docs/api-spec</c> and the readers and writers for them, shared by the commands and the contract tests.</summary>
public static class SpecFiles
{
    /// <summary>The merged OpenAPI document.</summary>
    public const string MergedSpec = "smtp2go-v3.json";

    /// <summary>The compact per-operation summary the contract tests read.</summary>
    public const string Endpoints = "endpoints.json";

    /// <summary>The allow-list of documented things the library deliberately does not model, each with a reason.</summary>
    public const string KnownUnmodelled = "known-unmodelled.json";

    /// <summary>The docs changelog page as of the harvest.</summary>
    public const string ChangelogSnapshot = "changelog-snapshot.md";

    /// <summary>What the OpenAPI validator reported for <see cref="MergedSpec"/>; written by <c>harvest</c>.</summary>
    public const string Validation = "validation.json";

    /// <summary>The hand-maintained list of validator findings the project accepts, each with a reason.</summary>
    public const string ValidationBaseline = "validation-baseline.json";

    /// <summary>Reads <see cref="Endpoints"/> from <paramref name="specDirectory"/>.</summary>
    public static EndpointsDocument ReadEndpoints(string specDirectory)
    {
        return ParseEndpoints(File.ReadAllText(Path.Combine(specDirectory, Endpoints)));
    }

    /// <summary>Parses the JSON text of an <see cref="Endpoints"/> file.</summary>
    public static EndpointsDocument ParseEndpoints(string json)
    {
        return JsonSerializer.Deserialize(json, HarvesterJsonContext.Default.EndpointsDocument)
            ?? throw new InvalidDataException("endpoints.json is empty.");
    }

    /// <summary>Reads <see cref="KnownUnmodelled"/> from <paramref name="specDirectory"/>; an absent file means an empty allow-list.</summary>
    public static KnownUnmodelledDocument ReadKnownUnmodelled(string specDirectory)
    {
        string path = Path.Combine(specDirectory, KnownUnmodelled);
        return File.Exists(path) ? ParseKnownUnmodelled(File.ReadAllText(path)) : new KnownUnmodelledDocument();
    }

    /// <summary>Parses the JSON text of a <see cref="KnownUnmodelled"/> file.</summary>
    public static KnownUnmodelledDocument ParseKnownUnmodelled(string json)
    {
        return JsonSerializer.Deserialize(json, HarvesterJsonContext.Default.KnownUnmodelledDocument)
            ?? throw new InvalidDataException("known-unmodelled.json is empty.");
    }

    /// <summary>Reads <see cref="MergedSpec"/> as text, with LF line endings, exactly as <c>harvest</c> validated it.</summary>
    public static string ReadMergedSpec(string specDirectory)
    {
        return File.ReadAllText(Path.Combine(specDirectory, MergedSpec)).Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    /// <summary>Reads <see cref="Validation"/> from <paramref name="specDirectory"/>; <c>null</c> when the file does not exist.</summary>
    public static ValidationDocument? ReadValidation(string specDirectory)
    {
        string path = Path.Combine(specDirectory, Validation);
        return File.Exists(path)
            ? JsonSerializer.Deserialize(File.ReadAllText(path), HarvesterJsonContext.Default.ValidationDocument) ?? throw new InvalidDataException("validation.json is empty.")
            : null;
    }

    /// <summary>Reads <see cref="ValidationBaseline"/> from <paramref name="specDirectory"/>; an absent file means nothing is accepted.</summary>
    public static ValidationBaselineDocument ReadValidationBaseline(string specDirectory)
    {
        string path = Path.Combine(specDirectory, ValidationBaseline);
        return File.Exists(path)
            ? JsonSerializer.Deserialize(File.ReadAllText(path), HarvesterJsonContext.Default.ValidationBaselineDocument) ?? throw new InvalidDataException("validation-baseline.json is empty.")
            : new ValidationBaselineDocument();
    }

    /// <summary>Serialises <paramref name="document"/> the way <c>harvest</c> writes <see cref="Validation"/>.</summary>
    public static string ToJson(ValidationDocument document)
    {
        return JsonSerializer.Serialize(document, HarvesterJsonContext.File.ValidationDocument) + "\n";
    }

    /// <summary>Serialises <paramref name="document"/> the way <c>harvest</c> writes it (indented, LF, trailing newline).</summary>
    public static string ToJson(EndpointsDocument document)
    {
        return JsonSerializer.Serialize(document, HarvesterJsonContext.File.EndpointsDocument) + "\n";
    }

    /// <summary>Serialises a raw JSON tree the way <c>harvest</c> writes <see cref="MergedSpec"/>.</summary>
    public static string ToJson(JsonNode node)
    {
        return node.ToJsonString(HarvesterJsonContext.FileOptions) + "\n";
    }

    /// <summary>Writes <paramref name="content"/> with LF line endings, creating the directory if needed.</summary>
    public static void Write(string directory, string fileName, string content)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, fileName), content.Replace("\r\n", "\n", StringComparison.Ordinal));
    }
}
