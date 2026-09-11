using System.Runtime.CompilerServices;

namespace Scott.Mail.Smtp2Go.AspNetCore;

/// <summary>Argument guards. The core and dependency-injection packages have their own internal copies.</summary>
internal static class Argument
{
    public static void ThrowIfNull([System.Diagnostics.CodeAnalysis.NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(argument, paramName);
    }

    public static void ThrowIfNullOrEmpty([System.Diagnostics.CodeAnalysis.NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
    }
}
