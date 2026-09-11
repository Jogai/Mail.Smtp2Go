using Scott.Mail.Smtp2Go;

// Deliberately outside the Scott.Mail.Smtp2Go namespace so the README's `using Scott.Mail.Smtp2Go;` line above is required, exactly as it is for a consumer.
namespace ReadmeSamples;

/// <summary>Holds the README quick start verbatim so the compiler keeps it honest. The body between the markers is compared with the README by <see cref="ReadmeQuickStartTests"/>.</summary>
public static class QuickStart
{
    public static async Task RunAsync()
    {
        // <quick-start>
        var client = new Smtp2GoClient(apiKey: "api-...");

        var response = await client.Email.SendAsync(new EmailSendRequest
        {
            Sender = "Alice <alice@example.com>",
            To = ["bob@example.com"],
            Subject = "Hello from Scott.Mail.Smtp2Go",
            TextBody = "It works.",
        });

        Console.WriteLine($"Sent: {response.Data.Succeeded} succeeded, {response.Data.Failed} failed ({response.RequestId})");
        // </quick-start>
    }
}

public class ReadmeQuickStartTests
{
    [Fact]
    public void Quick_start_compiles_and_is_an_async_method()
    {
        typeof(QuickStart).GetMethod(nameof(QuickStart.RunAsync))!.ReturnType.Should().Be<Task>();
    }

    /// <summary>The repository README and the NuGet package README (docs/nuget-readme.md, packed as README.md) carry the same quick start.</summary>
    [Theory]
    [InlineData("README.md")]
    [InlineData("docs/nuget-readme.md")]
    public void Readme_code_block_matches_the_compiled_quick_start(string relativePath)
    {
        string source = File.ReadAllText(ThisFile());
        string readme = File.ReadAllText(Path.Combine(RepositoryRoot(), relativePath));

        IReadOnlyList<string> readmeUsings = Lines(CodeBlockAfter(readme, "## Quick start")).Where(l => l.StartsWith("using ", StringComparison.Ordinal)).ToList();
        IReadOnlyList<string> readmeBody = Lines(CodeBlockAfter(readme, "## Quick start")).Where(l => !l.StartsWith("using ", StringComparison.Ordinal)).ToList();
        IReadOnlyList<string> testBody = Lines(Between(source, "// <quick-start>", "// </quick-start>"));

        readmeUsings.Should().NotBeEmpty().And.AllSatisfy(u => source.Should().Contain(u, because: "the using directives of {0} must appear in this test file", relativePath));
        testBody.Should().Equal(readmeBody, because: "the quick start in {0} and the compiled copy in {1} must be identical", relativePath, nameof(QuickStart));
    }

    /// <summary>
    ///   Path of this source file, derived from the repository root rather than <c>[CallerFilePath]</c>: deterministic builds
    ///   (<c>ContinuousIntegrationBuild</c>, on for every GitHub Actions run) rewrite caller paths to <c>/_/...</c>.
    /// </summary>
    private static string ThisFile()
    {
        return Path.Combine(RepositoryRoot(), "test", "Scott.Mail.Smtp2Go.Tests.Unit", "Email", "ReadmeQuickStartTests.cs");
    }

    /// <summary>Walks up from the test assembly's output directory to the directory that holds the solution file.</summary>
    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Scott.Mail.Smtp2Go.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new FileNotFoundException("Scott.Mail.Smtp2Go.slnx not found above the test output directory " + AppContext.BaseDirectory);
    }

    private static string CodeBlockAfter(string markdown, string heading)
    {
        int start = markdown.IndexOf(heading, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, because: "the README must have a '{0}' heading", heading);
        return Between(markdown.Substring(start), "```csharp", "```");
    }

    private static string Between(string text, string open, string close)
    {
        int start = text.IndexOf(open, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, because: "'{0}' must be present", open);
        start += open.Length;
        int end = text.IndexOf(close, start, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start, because: "'{0}' must be present after '{1}'", close, open);
        return text.Substring(start, end - start);
    }

    /// <summary>Non-blank lines with surrounding whitespace removed, so indentation depth does not matter.</summary>
    private static List<string> Lines(string block)
    {
        return block.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
    }
}
