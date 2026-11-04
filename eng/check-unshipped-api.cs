#!/usr/bin/env dotnet
#:property RestorePackagesWithLockFile=false
// Only ever run with `dotnet run`, never published: no native AOT, so the ILCompiler package (and its audit findings) stays out.
#:property PublishAot=false
// Fails when any PublicAPI.Unshipped.txt under src/ still lists API surface. A release ships everything: the entries move to the
// PublicAPI.Shipped.txt next to each file (docs/contributing.md, "Releasing"). The publish workflow runs this on every tag build.
// Usage: dotnet run eng/check-unshipped-api.cs

string root = RepoRoot.Find();
string src = Path.Combine(root, "src");
int status = 0;

foreach (string file in Directory.EnumerateFiles(src, "PublicAPI.Unshipped.txt", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
{
    // File.ReadAllLines drops the byte-order mark and CR line endings; the analyzer's `#nullable enable` header and blank lines are not entries.
    string[] pending = File.ReadAllLines(file).Where(line => line != "#nullable enable" && !string.IsNullOrWhiteSpace(line)).ToArray();
    if (pending.Length == 0)
    {
        continue;
    }

    string relative = Path.GetRelativePath(root, file).Replace('\\', '/');
    Console.WriteLine($"::error file={relative}::{pending.Length} unshipped public API entries; move them to PublicAPI.Shipped.txt before tagging a release.");
    foreach (string entry in pending.Take(20))
    {
        Console.WriteLine(entry);
    }

    status = 1;
}

if (status == 0)
{
    Console.WriteLine("All PublicAPI.Unshipped.txt files are empty.");
}

return status;

/// <summary>The repository root: the nearest directory up from the working directory that holds the solution file.</summary>
static class RepoRoot
{
    public static string Find()
    {
        for (DirectoryInfo? directory = new(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Scott.Mail.Smtp2Go.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Run this from inside the repository: no Scott.Mail.Smtp2Go.slnx found in the current directory or any parent.");
    }
}
