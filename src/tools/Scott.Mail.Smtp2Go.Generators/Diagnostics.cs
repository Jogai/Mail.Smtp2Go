using Microsoft.CodeAnalysis;

namespace Scott.Mail.Smtp2Go.Generators;

/// <summary>The diagnostics the generators report. Listed in AnalyzerReleases.Unshipped.md (release tracking, RS2008).</summary>
internal static class Diagnostics
{
    private const string Category = "Smtp2Go.Generation";

    public static readonly DiagnosticDescriptor SnapshotMissing = new(
        "SMTP2GO001",
        "Endpoint snapshot not found",
        "No additional file named '{0}' is passed to the compiler; add docs/api-spec/endpoints.json as an AdditionalFiles item",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SnapshotUnreadable = new(
        "SMTP2GO002",
        "Endpoint snapshot unreadable",
        "The endpoint snapshot '{0}' could not be read: {1}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TraitsPatternUnmatched = new(
        "SMTP2GO003",
        "Endpoint traits pattern matches nothing",
        "The EndpointTraits pattern '{0}' matches no operation in the endpoint snapshot; fix or remove it",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotPartial = new(
        "SMTP2GO004",
        "Generated type must be partial",
        "'{0}' is marked for code generation but is not declared partial",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoConnection = new(
        "SMTP2GO005",
        "Generated client has no connection",
        "'{0}' is marked with [GeneratedClient] but has no constructor parameter of type Smtp2GoConnection",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
