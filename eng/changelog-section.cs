#!/usr/bin/env dotnet
#:property RestorePackagesWithLockFile=false
// Only ever run with `dotnet run`, never published: no native AOT, so the ILCompiler package (and its audit findings) stays out.
#:property PublishAot=false
// Prints the body of one version's section of CHANGELOG.md (Keep a Changelog layout), for the GitHub release notes.
// Usage: dotnet run eng/changelog-section.cs -- <version> [--out <file>]
// Writes to stdout unless --out is given. Exit code 1 when the section does not exist.

using System.Text;

if (args.Length is 0 or > 3 || (args.Length == 2) || (args.Length == 3 && args[1] != "--out"))
{
    Console.Error.WriteLine("usage: dotnet run eng/changelog-section.cs -- <version> [--out <file>]");
    return 2;
}

string version = args[0];
string? outFile = args.Length == 3 ? args[2] : null;
string root = RepoRoot.Find();
string changelog = Path.Combine(root, "CHANGELOG.md");
string heading = $"## [{version}]";

List<string> section = [];
bool printing = false;
foreach (string line in File.ReadAllLines(changelog))
{
    if (line.StartsWith("## [", StringComparison.Ordinal))
    {
        printing = line.StartsWith(heading, StringComparison.Ordinal);
        continue;
    }

    // The link-reference definitions at the end of the file ([1.3.5]: https://...) are not part of the last section.
    if (printing && !IsLinkReference(line))
    {
        section.Add(line.TrimEnd('\r'));
    }
}

while (section.Count > 0 && section[0].Length == 0)
{
    section.RemoveAt(0);
}

while (section.Count > 0 && section[^1].Length == 0)
{
    section.RemoveAt(section.Count - 1);
}

if (section.Count == 0)
{
    Console.Error.WriteLine($"::error file=CHANGELOG.md::No section '{heading}' in CHANGELOG.md");
    return 1;
}

string body = string.Join("\n", section) + "\n";
if (outFile is null)
{
    Console.Out.Write(body);
}
else
{
    File.WriteAllText(outFile, body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    Console.WriteLine($"Wrote the {version} section ({section.Count} lines) to {outFile}.");
}

return 0;

static bool IsLinkReference(string line)
{
    // "[name]: url" at the start of the line.
    if (line.Length < 4 || line[0] != '[')
    {
        return false;
    }

    int close = line.IndexOf(']', StringComparison.Ordinal);
    return close > 1 && close + 2 < line.Length && line[close + 1] == ':' && line[close + 2] == ' ';
}

/// <summary>The repository root: the nearest directory up from the working directory that holds the solution file. The scripts are run from anywhere inside the checkout.</summary>
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
