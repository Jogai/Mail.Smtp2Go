using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Scott.Mail.Smtp2Go.Generators;

namespace Scott.Mail.Smtp2Go.Tests.Generators;

/// <summary>What one generator run produced: the compilation with the generated sources added, the generator's diagnostics and the sources by hint name.</summary>
public sealed record GeneratorRun(Compilation Output, ImmutableArray<Diagnostic> Diagnostics, IReadOnlyDictionary<string, string> Sources)
{
    /// <summary>The generated source whose hint name ends with <paramref name="hintSuffix"/>.</summary>
    public string Source(string hintSuffix)
    {
        return Sources.Single(pair => pair.Key.EndsWith(hintSuffix, StringComparison.Ordinal)).Value;
    }

    /// <summary>The errors of the output compilation, which should be none when the generated code is well-formed against the stubs.</summary>
    public IEnumerable<Diagnostic> CompileErrors()
    {
        return Output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
    }
}

/// <summary>Runs <see cref="Smtp2GoGenerator"/> in-process over the stub library plus test sources, with an optional endpoint snapshot as the additional file.</summary>
public static class GeneratorHost
{
    private static readonly ImmutableArray<MetadataReference> s_references = LoadReferences();

    public static GeneratorRun Run(string source, string? endpointsJson = null, string snapshotFileName = "endpoints.json")
    {
        CSharpParseOptions parseOptions = new(LanguageVersion.Latest);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Scott.Mail.Smtp2Go.Stubs",
            [CSharpSyntaxTree.ParseText(Stubs.Library, parseOptions, path: "Stubs.cs"), CSharpSyntaxTree.ParseText(source, parseOptions, path: "Test.cs")],
            s_references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        GeneratorDriver driver = CSharpGeneratorDriver.Create([new Smtp2GoGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        if (endpointsJson is not null)
        {
            driver = driver.AddAdditionalTexts([new InMemoryText("/repo/docs/api-spec/" + snapshotFileName, endpointsJson)]);
        }

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out ImmutableArray<Diagnostic> diagnostics);
        GeneratorDriverRunResult result = driver.GetRunResult();
        Dictionary<string, string> sources = result.Results
            .SelectMany(r => r.GeneratedSources)
            .ToDictionary(s => s.HintName, s => s.SourceText.ToString(), StringComparer.Ordinal);
        return new GeneratorRun(output, diagnostics, sources);
    }

    private static ImmutableArray<MetadataReference> LoadReferences()
    {
        string[] paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        return paths
            .Where(p => Path.GetFileName(p) is "System.Runtime.dll" or "System.Private.CoreLib.dll" or "netstandard.dll" or "System.Collections.dll" or "System.Net.Http.dll" or "System.Net.Primitives.dll" or "System.Text.Json.dll" or "System.Threading.dll" or "System.Threading.Tasks.dll" or "System.Linq.dll" or "System.Memory.dll" or "System.Runtime.Extensions.dll")
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToImmutableArray();
    }

    private sealed class InMemoryText(string path, string text) : AdditionalText
    {
        public override string Path => path;

        public override SourceText? GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From(text);
        }
    }
}
