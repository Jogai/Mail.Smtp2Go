using Scott.Mail.Smtp2Go.SpecHarvester;

namespace Scott.Mail.Smtp2Go.Tests.Contract;

/// <summary>
/// The merged OpenAPI document is validated before it is written. <c>validation.json</c> records what the validator found,
/// <c>validation-baseline.json</c> records what the project accepts, and nothing else may appear.
/// </summary>
public class SpecValidationTests
{
    private const string Minimal = """{"openapi":"3.1.0","info":{"title":"t","version":"1"},"paths":{"/a":{"post":{"operationId":"a","responses":{"200":{"description":"ok"}}}}}}""";

    private static ValidationDocument Current { get; } = SpecValidator.Validate(SpecFiles.ReadMergedSpec(Spec.Directory));

    private static ValidationBaselineDocument Baseline { get; } = SpecFiles.ReadValidationBaseline(Spec.Directory);

    [Fact]
    public void Validation_file_is_what_the_validator_reports_for_the_committed_spec()
    {
        string committed = File.ReadAllText(Path.Combine(Spec.Directory, SpecFiles.Validation)).Replace("\r\n", "\n", StringComparison.Ordinal);

        committed.Should().Be(
            SpecFiles.ToJson(Current),
            because: "validation.json is generated; run the harvester's validate command with --write (or re-harvest) after the spec or the validator changes");
    }

    [Fact]
    public void Committed_spec_is_read_as_OpenAPI_3()
    {
        Current.SpecificationVersion.Should().StartWith("3.");
    }

    [Fact]
    public void Every_finding_is_accepted_by_the_baseline()
    {
        Baseline.Unaccepted(Current).Select(f => $"{f.Severity} {f.Location}: {f.Message}").Should().BeEmpty(
            because: "a new validator finding needs a look: fix the merge if it is ours, or add it to validation-baseline.json with a reason if it is upstream");
    }

    [Fact]
    public void Baseline_entries_are_still_needed_and_carry_a_reason()
    {
        Baseline.Unneeded(Current).Select(a => a.Location).Should().BeEmpty(because: "a baseline entry that matches no finding any more must be deleted");
        Baseline.Accepted.Should().OnlyContain(a => !string.IsNullOrWhiteSpace(a.Reason));
    }

    [Fact]
    public void A_valid_document_has_no_findings()
    {
        ValidationDocument report = SpecValidator.Validate(Minimal);

        report.SpecificationVersion.Should().Be("3.1");
        report.Findings.Should().BeEmpty();
    }

    [Theory]
    [InlineData("""{"openapi":"3.1.0","paths":{"/a":{"post":{"operationId":"a","responses":{"200":{"description":"ok"}}}}}}""", "InfoRequiredFields", "#/info/title")]
    [InlineData("""{"openapi":"3.1.0","info":{"title":"t","version":"1"},"paths":{"/a":{"post":{"operationId":"a","responses":{"200":{}}}}}}""", "ResponseRequiredFields", "#/paths/~1a/post/responses/200/description")]
    [InlineData("""{"openapi":"3.1.0","info":{"title":"t","version":"1"},"paths":{"/a":{"post":{"operationId":"x","responses":{"200":{"description":"ok"}}}},"/b":{"post":{"operationId":"x","responses":{"200":{"description":"ok"}}}}}}""", SpecValidator.UniqueOperationIdsRule, "#/paths/~1b/post/operationId")]
    [InlineData("""{"openapi":"3.1.0","info":{"title":"t","version":"1"},"paths":{}}""", SpecValidator.HasOperationsRule, "#/paths")]
    [InlineData("""{"swagger":"2.0","info":{"title":"t","version":"1"},"paths":{"/a":{"post":{"operationId":"a","responses":{"200":{"description":"ok"}}}}}}""", SpecValidator.ExpectsOpenApi3Rule, "#/openapi")]
    public void Broken_documents_are_reported_with_rule_and_location(string document, string rule, string location)
    {
        ValidationDocument report = SpecValidator.Validate(document);

        report.Findings.Should().Contain(f => f.Severity == SpecValidator.Error && f.Rule == rule && f.Location == location);
    }

    [Theory]
    [InlineData("""{"openapi": """)]
    [InlineData("[1,2,3]")]
    [InlineData("")]
    public void Unreadable_input_is_a_finding_not_an_exception(string document)
    {
        ValidationDocument report = SpecValidator.Validate(document);

        report.SpecificationVersion.Should().BeNull();
        report.Findings.Should().NotBeEmpty().And.OnlyContain(f => f.Severity == SpecValidator.Error);
    }

    [Fact]
    public void Findings_are_ordered_and_serialise_deterministically()
    {
        const string twoProblems = """{"openapi":"3.1.0","info":{"version":"1"},"paths":{"/b":{"post":{"operationId":"x","responses":{"200":{"description":"ok"}}}},"/a":{"post":{"operationId":"x","responses":{"200":{}}}}}}""";

        ValidationDocument first = SpecValidator.Validate(twoProblems);
        ValidationDocument second = SpecValidator.Validate(twoProblems);

        first.Findings.Select(f => f.Location).Should().BeInAscendingOrder(StringComparer.Ordinal);
        SpecFiles.ToJson(first).Should().Be(SpecFiles.ToJson(second)).And.EndWith("\n");
        SpecFiles.ToJson(first).Should().NotContain("\"key\"", because: "the comparison key is derived, not stored");
    }

    [Fact]
    public void Baseline_separates_accepted_new_and_obsolete_findings()
    {
        ValidationDocument report = SpecValidator.Validate("""{"openapi":"3.1.0","info":{"title":"t","version":"1"},"paths":{}}""");
        ValidationFinding finding = report.Findings.Should().ContainSingle().Subject;
        AcceptedFinding accepts = new() { Severity = finding.Severity, Rule = finding.Rule, Location = finding.Location, Message = finding.Message, Reason = "test" };
        AcceptedFinding obsolete = accepts with { Location = "#/gone" };

        new ValidationBaselineDocument().Unaccepted(report).Should().ContainSingle();
        new ValidationBaselineDocument { Accepted = [accepts] }.Unaccepted(report).Should().BeEmpty();
        new ValidationBaselineDocument { Accepted = [accepts, obsolete] }.Unneeded(report).Should().Equal(obsolete);
    }
}
