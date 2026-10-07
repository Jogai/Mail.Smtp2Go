#!/usr/bin/env dotnet
#:property RestorePackagesWithLockFile=false
// Inspects every .nupkg in a directory (default: artifacts/) and fails unless each one carries the README and icon named in its nuspec,
// the LGPL-3.0-or-later license expression, the repository url with a commit, at least one lib/ assembly, and has a symbols package next to it.
// Usage: dotnet run eng/verify-nupkg.cs -- [directory]

using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;

string dir = args.Length > 0 ? args[0] : "artifacts";
string[] packages = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.nupkg").Order(StringComparer.Ordinal).ToArray() : [];
if (packages.Length == 0)
{
    Console.WriteLine($"::error::No .nupkg files in {dir}");
    return 1;
}

int status = 0;
foreach (string package in packages)
{
    Console.WriteLine($"== {package}");
    using ZipArchive archive = ZipFile.OpenRead(package);
    string[] listing = archive.Entries.Select(e => e.FullName).ToArray();
    ZipArchiveEntry nuspecEntry = archive.Entries.Single(e => e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase) && !e.FullName.Contains('/', StringComparison.Ordinal));
    XElement metadata;
    using (Stream stream = nuspecEntry.Open())
    {
        metadata = XDocument.Load(stream).Root!.Elements().Single(e => e.Name.LocalName == "metadata");
    }

    string? Element(string name) => metadata.Elements().SingleOrDefault(e => e.Name.LocalName == name)?.Value;
    void Fail(string message)
    {
        Console.WriteLine($"::error file={package}::{message}");
        status = 1;
    }

    string? readme = Element("readme");
    string? icon = Element("icon");
    XElement? license = metadata.Elements().SingleOrDefault(e => e.Name.LocalName == "license");
    XElement? repository = metadata.Elements().SingleOrDefault(e => e.Name.LocalName == "repository");

    if (string.IsNullOrEmpty(readme) || !listing.Contains(readme, StringComparer.Ordinal))
    {
        Fail($"readme '{readme}' missing from the package");
    }

    if (string.IsNullOrEmpty(icon) || !listing.Contains(icon, StringComparer.Ordinal))
    {
        Fail($"icon '{icon}' missing from the package");
    }

    if (license is null || license.Attribute("type")?.Value != "expression" || license.Value != "LGPL-3.0-or-later")
    {
        Fail("license expression is not LGPL-3.0-or-later");
    }

    if (repository is null || repository.Attribute("type")?.Value != "git" || string.IsNullOrEmpty(repository.Attribute("url")?.Value)
        || !Regex.IsMatch(repository.Attribute("commit")?.Value ?? string.Empty, "^[0-9a-f]{40}$"))
    {
        Fail("repository url or commit missing");
    }

    if (!listing.Any(entry => Regex.IsMatch(entry, @"^lib/[^/]+/.*\.dll$")))
    {
        Fail("no lib/ assembly");
    }

    if (!File.Exists(Path.ChangeExtension(package, ".snupkg")))
    {
        Fail("symbols package missing");
    }

    foreach (string name in new[] { "id", "version", "readme", "icon" })
    {
        Console.WriteLine($"  {name}: {Element(name)}");
    }

    Console.WriteLine($"  license: {license?.Value}");
    foreach (string entry in listing.Where(e => e.StartsWith("lib/", StringComparison.Ordinal) || e.StartsWith("README", StringComparison.Ordinal) || e.StartsWith("docs/", StringComparison.Ordinal) || e.StartsWith("icon", StringComparison.Ordinal)))
    {
        Console.WriteLine($"  {entry}");
    }
}

return status;
