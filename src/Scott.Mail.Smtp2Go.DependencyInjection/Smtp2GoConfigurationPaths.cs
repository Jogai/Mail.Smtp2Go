using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Scott.Mail.Smtp2Go.DependencyInjection;

/// <summary>Remembers which configuration path each named <see cref="Smtp2GoOptions"/> was bound from, so validation messages can name it.</summary>
internal sealed class Smtp2GoConfigurationPaths
{
    /// <summary>The path used when options were configured in code rather than bound from a section.</summary>
    public const string DefaultPath = "Smtp2Go";

    private readonly ConcurrentDictionary<string, string> _paths = new(StringComparer.Ordinal);

    public void Set(string? name, string path)
    {
        _paths[name ?? Options.DefaultName] = path;
    }

    public string Get(string? name)
    {
        string key = name ?? Options.DefaultName;
        if (_paths.TryGetValue(key, out string? path))
        {
            return path;
        }

        return key.Length == 0 ? DefaultPath : DefaultPath + ":" + key;
    }
}
