using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go.SpecHarvester;

/// <summary>One validator finding the project has looked at and accepted, with the reason. Hand-maintained in <c>validation-baseline.json</c>.</summary>
public sealed record AcceptedFinding
{
    /// <summary><c>error</c> or <c>warning</c>, as in <c>validation.json</c>.</summary>
    public required string Severity { get; init; }

    /// <summary>The rule name, when the finding has one.</summary>
    public string? Rule { get; init; }

    /// <summary>The JSON pointer of the finding.</summary>
    public required string Location { get; init; }

    /// <summary>The validator's message, verbatim.</summary>
    public required string Message { get; init; }

    /// <summary>Why the finding is acceptable. Required.</summary>
    public required string Reason { get; init; }

    /// <summary>The key of the <see cref="ValidationFinding"/> this entry accepts.</summary>
    [JsonIgnore]
    public string Key => string.Join('\u001f', Severity, Rule ?? string.Empty, Location, Message);
}

/// <summary>The content of <c>validation-baseline.json</c>: the findings that may appear in <c>validation.json</c> without failing a check.</summary>
public sealed record ValidationBaselineDocument
{
    /// <summary>The accepted findings.</summary>
    public IReadOnlyList<AcceptedFinding> Accepted { get; init; } = [];

    /// <summary>The findings of <paramref name="report"/> that no baseline entry accepts.</summary>
    public IReadOnlyList<ValidationFinding> Unaccepted(ValidationDocument report)
    {
        ArgumentNullException.ThrowIfNull(report);
        HashSet<string> accepted = Accepted.Select(a => a.Key).ToHashSet(StringComparer.Ordinal);
        return report.Findings.Where(f => !accepted.Contains(f.Key)).ToList();
    }

    /// <summary>The baseline entries that match no finding of <paramref name="report"/> any more and should be deleted.</summary>
    public IReadOnlyList<AcceptedFinding> Unneeded(ValidationDocument report)
    {
        ArgumentNullException.ThrowIfNull(report);
        HashSet<string> present = report.Findings.Select(f => f.Key).ToHashSet(StringComparer.Ordinal);
        return Accepted.Where(a => !present.Contains(a.Key)).ToList();
    }
}
