using Microsoft.Extensions.Options;

namespace Scott.Mail.Smtp2Go.DependencyInjection;

/// <summary>Maps a registration name to the <see cref="IHttpClientFactory"/> client name: <c>Scott.Mail.Smtp2Go</c> or <c>Scott.Mail.Smtp2Go:{name}</c>.</summary>
internal static class Smtp2GoHttpClientNames
{
    public const string Prefix = "Scott.Mail.Smtp2Go";

    public static string For(string? name)
    {
        return string.IsNullOrEmpty(name) || name == Options.DefaultName ? Prefix : Prefix + ":" + name;
    }
}
