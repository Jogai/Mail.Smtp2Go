using System.Runtime.CompilerServices;

namespace Scott.Mail.Smtp2Go.DependencyInjection;

/// <summary>Argument guards. The core package has its own internal copy.</summary>
internal static class Argument
{
    public static void ThrowIfNull([System.Diagnostics.CodeAnalysis.NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(argument, paramName);
    }
}
