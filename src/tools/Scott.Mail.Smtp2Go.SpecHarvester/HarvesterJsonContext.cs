using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go.SpecHarvester;

/// <summary>Source-generated serializer context for the files under <c>docs/api-spec</c>: camelCase, indented, nulls omitted.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(EndpointsDocument))]
[JsonSerializable(typeof(KnownUnmodelledDocument))]
[JsonSerializable(typeof(JsonNode))]
internal sealed partial class HarvesterJsonContext : JsonSerializerContext
{
    // Lazily created: static field initializers in a partial class run in an unspecified order across files, and the generated part owns Default.
    private static JsonSerializerOptions? s_fileOptions;
    private static HarvesterJsonContext? s_file;

    /// <summary>Options for writing the files: two-space indent, LF line endings, HTML and non-ASCII text left unescaped, so diffs stay readable.</summary>
    public static JsonSerializerOptions FileOptions => s_fileOptions ??= new JsonSerializerOptions(Default.Options)
    {
        WriteIndented = true,
        IndentSize = 2,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>A context bound to <see cref="FileOptions"/>.</summary>
    public static HarvesterJsonContext File => s_file ??= new HarvesterJsonContext(FileOptions);
}
