using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Nodes;
using Scott.Mail.Smtp2Go.SpecHarvester;
using Scott.Mail.Smtp2Go.Transport;

const string DefaultSource = "https://developers.smtp2go.com/";
const string DefaultSpecDirectory = "docs/api-spec";
const string DefaultCoverageFile = "docs/api-coverage.md";

Option<string> specOption = new("--spec") { Description = "The docs/api-spec directory.", DefaultValueFactory = _ => DefaultSpecDirectory };

// harvest
Option<string> outOption = new("--out") { Description = "Directory to write the snapshot to.", DefaultValueFactory = _ => DefaultSpecDirectory };
Option<string> sourceOption = new("--source") { Description = "Site root URL, or a directory mirroring it (llms.txt, reference/*.md, docs/*.md).", DefaultValueFactory = _ => DefaultSource };
Option<string?> cacheOption = new("--cache") { Description = "Directory to save downloaded pages to and read them back from on later runs." };
Command harvest = new("harvest", "Download every reference page, extract the OpenAPI fragments and write smtp2go-v3.json, endpoints.json and changelog-snapshot.md.")
{
    outOption,
    sourceOption,
    cacheOption,
};
harvest.SetAction((parseResult, cancellationToken) => HarvestAsync(parseResult.GetValue(outOption)!, parseResult.GetValue(sourceOption)!, parseResult.GetValue(cacheOption), cancellationToken));

// diff
Option<string> againstOption = new("--against") { Description = "A git ref whose endpoints.json to compare with, or a path to an endpoints.json file.", DefaultValueFactory = _ => "HEAD" };
Option<string?> diffOutOption = new("--out") { Description = "Write the Markdown report to this file instead of stdout." };
Command diff = new("diff", "Print the added, removed and changed operations between a committed endpoints.json and the working copy, as Markdown.")
{
    againstOption,
    specOption,
    diffOutOption,
};
diff.SetAction((parseResult, cancellationToken) => DiffAsync(parseResult.GetValue(againstOption)!, parseResult.GetValue(specOption)!, parseResult.GetValue(diffOutOption), cancellationToken));

// coverage
Option<string> coverageOutOption = new("--out") { Description = "The Markdown file to write.", DefaultValueFactory = _ => DefaultCoverageFile };
Option<bool> checkOption = new("--check") { Description = "Do not write; exit 1 when the file on disk differs from what would be generated." };
Command coverage = new("coverage", "Join endpoints.json with the built core assembly (EndpointTable, [Smtp2GoEndpoint] models, client interfaces) into docs/api-coverage.md.")
{
    specOption,
    coverageOutOption,
    checkOption,
};
coverage.SetAction(parseResult => Coverage(parseResult.GetValue(specOption)!, parseResult.GetValue(coverageOutOption)!, parseResult.GetValue(checkOption)));

RootCommand root = new("Harvests the SMTP2GO v3 API specification from developers.smtp2go.com and reports library coverage.") { harvest, diff, coverage };
return await root.Parse(args).InvokeAsync().ConfigureAwait(false);

static async Task<int> HarvestAsync(string outDirectory, string source, string? cache, CancellationToken cancellationToken)
{
    using PageSource pages = new(source, cache);
    Console.WriteLine($"Harvesting from {pages.Description}");

    string llms = await pages.GetAsync(LlmsIndex.RelativePath, cancellationToken).ConfigureAwait(false);
    IReadOnlyList<IndexEntry> entries = LlmsIndex.ParseReferenceSection(llms);
    if (entries.Count == 0)
    {
        Console.Error.WriteLine("llms.txt has no '## API Reference' section; nothing to harvest.");
        return 2;
    }

    Console.WriteLine($"{entries.Count} reference pages listed");

    Task<PageFragment>[] downloads = entries
        .Select(async entry => FragmentExtractor.Extract(entry.Slug, await pages.GetAsync(entry.RelativePath, cancellationToken).ConfigureAwait(false)))
        .ToArray();
    PageFragment[] fragments = await Task.WhenAll(downloads).ConfigureAwait(false);
    string overview = await pages.GetAsync(LlmsIndex.WebhooksOverviewPath, cancellationToken).ConfigureAwait(false);
    string changelog = await pages.GetAsync(LlmsIndex.ChangelogPath, cancellationToken).ConfigureAwait(false);

    List<OperationSummary> operations = [];
    List<PageNote> unparsed = [];
    List<string> withoutSpec = [];
    List<JsonObject> specs = [];
    for (int i = 0; i < fragments.Length; i++)
    {
        PageFragment page = fragments[i];
        if (page.Spec is not null)
        {
            specs.Add(page.Spec);
        }
        else if (page.HasFragment)
        {
            Console.Error.WriteLine($"warning: {page.Slug}: OpenAPI block did not parse: {page.Error}");
            unparsed.Add(new PageNote { Page = page.Slug, Title = entries[i].Title, Description = page.Prose, Error = page.Error ?? "unknown" });
        }
        else
        {
            withoutSpec.Add(page.Slug);
        }

        operations.AddRange(OperationSummarizer.Summarize(page));
    }

    JsonObject merged = SpecMerger.Merge(specs, message => Console.Error.WriteLine("warning: " + message));
    string? apiVersion = string.Join(",", specs.Select(OperationSummarizer.ApiVersion).Where(v => v is not null).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));

    EndpointsDocument endpoints = new()
    {
        Source = pages.Description.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? pages.Description + LlmsIndex.RelativePath : DefaultSource + LlmsIndex.RelativePath,
        ApiVersion = string.IsNullOrEmpty(apiVersion) ? null : apiVersion,
        Operations = operations.OrderBy(o => o.Path, StringComparer.Ordinal).ThenBy(o => o.Method, StringComparer.Ordinal).ToList(),
        Callbacks = CallbackTables.Parse(overview),
        UnparsedPages = unparsed.OrderBy(p => p.Page, StringComparer.Ordinal).ToList(),
        PagesWithoutSpec = withoutSpec.Order(StringComparer.Ordinal).ToList(),
    };

    SpecFiles.Write(outDirectory, SpecFiles.MergedSpec, SpecFiles.ToJson(merged));
    SpecFiles.Write(outDirectory, SpecFiles.Endpoints, SpecFiles.ToJson(endpoints));
    SpecFiles.Write(outDirectory, SpecFiles.ChangelogSnapshot, ChangelogSnapshot(changelog));

    int parsedOperations = endpoints.Operations.Count(o => o.IsParsed);
    Console.WriteLine($"{parsedOperations} operations from {specs.Count} fragments (API v{endpoints.ApiVersion}); {unparsed.Count} unparsable, {withoutSpec.Count} without a fragment ({string.Join(", ", withoutSpec)})");
    foreach (CallbackSummary callback in endpoints.Callbacks)
    {
        Console.WriteLine($"{callback.Path}: {callback.Parameters.Count} parameters, {callback.Events.Count} events");
    }

    Console.WriteLine($"Written to {Path.GetFullPath(outDirectory)}");
    return 0;
}

static async Task<int> DiffAsync(string against, string specDirectory, string? outFile, CancellationToken cancellationToken)
{
    string currentPath = Path.Combine(specDirectory, SpecFiles.Endpoints);
    EndpointsDocument current = SpecFiles.ParseEndpoints(await File.ReadAllTextAsync(currentPath, cancellationToken).ConfigureAwait(false));

    string previousJson;
    if (File.Exists(against))
    {
        previousJson = await File.ReadAllTextAsync(against, cancellationToken).ConfigureAwait(false);
    }
    else
    {
        string? repoRelative = await GitAsync(["ls-files", "--full-name", currentPath], cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(repoRelative))
        {
            Console.Error.WriteLine($"{currentPath} is not tracked by git; pass --against <file> instead of a ref.");
            return 2;
        }

        previousJson = await GitAsync(["show", $"{against}:{repoRelative.Trim()}"], cancellationToken).ConfigureAwait(false) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(previousJson))
        {
            Console.Error.WriteLine($"git show {against}:{repoRelative.Trim()} returned nothing.");
            return 2;
        }
    }

    string report = SpecDiff.Markdown(SpecFiles.ParseEndpoints(previousJson), current);
    if (outFile is null)
    {
        Console.Write(report);
    }
    else
    {
        await File.WriteAllTextAsync(outFile, report, cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"Written to {Path.GetFullPath(outFile)} ({(SpecDiff.IsEmpty(report) ? "no changes" : "changes found")})");
    }

    return 0;
}

static int Coverage(string specDirectory, string outFile, bool check)
{
    EndpointsDocument endpoints = SpecFiles.ReadEndpoints(specDirectory);
    KnownUnmodelledDocument known = SpecFiles.ReadKnownUnmodelled(specDirectory);
    string report = CoverageReport.Generate(endpoints, known, typeof(EndpointTable).Assembly);

    if (check)
    {
        string existing = File.Exists(outFile) ? File.ReadAllText(outFile).Replace("\r\n", "\n", StringComparison.Ordinal) : string.Empty;
        if (string.Equals(existing, report, StringComparison.Ordinal))
        {
            Console.WriteLine($"{outFile} is up to date");
            return 0;
        }

        Console.Error.WriteLine($"{outFile} is stale; run the coverage command without --check to regenerate it.");
        return 1;
    }

    File.WriteAllText(outFile, report);
    IReadOnlyList<CoverageRow> rows = CoverageReport.Rows(endpoints, known, typeof(EndpointTable).Assembly);
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{rows.Count} operations: {rows.Count(r => r.Status == CoverageStatus.Typed)} typed, {rows.Count(r => r.Status == CoverageStatus.RawOnly)} raw-only, {rows.Count(r => r.Status == CoverageStatus.Pending)} pending, {rows.Count(r => r.Status == CoverageStatus.Missing)} missing"));
    Console.WriteLine($"Written to {Path.GetFullPath(outFile)}");
    return 0;
}

static string ChangelogSnapshot(string changelogMarkdown)
{
    PageFragment page = FragmentExtractor.Extract("changelog", changelogMarkdown);
    string header = "<!-- Snapshot of https://developers.smtp2go.com/reference/changelog taken by the spec harvester; page updatedAt " + (page.UpdatedAt ?? "unknown") + ". Do not edit. -->\n\n";
    return header + page.Prose + "\n";
}

static async Task<string?> GitAsync(string[] arguments, CancellationToken cancellationToken)
{
    ProcessStartInfo start = new("git") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
    foreach (string argument in arguments)
    {
        start.ArgumentList.Add(argument);
    }

    using Process? process = Process.Start(start);
    if (process is null)
    {
        return null;
    }

    string output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
    return process.ExitCode == 0 ? output : null;
}
