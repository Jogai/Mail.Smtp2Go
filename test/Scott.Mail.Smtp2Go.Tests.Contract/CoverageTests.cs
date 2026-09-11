using Scott.Mail.Smtp2Go.SpecHarvester;
using Scott.Mail.Smtp2Go.Tests.Shared;

namespace Scott.Mail.Smtp2Go.Tests.Contract;

/// <summary>
/// <c>docs/api-coverage.md</c> is what the harvester would generate now, and every <c>known-unmodelled.json</c> entry is still needed:
/// once a family lands, its whole-operation entries must be deleted and field entries must still name something the library lacks.
/// </summary>
public class CoverageTests
{
    public static TheoryData<string> Entries()
    {
        TheoryData<string> data = [];
        foreach (KnownUnmodelledEntry entry in Spec.Known.Entries)
        {
            data.Add(entry.ToString());
        }

        return data;
    }

    [Fact]
    public void Api_coverage_markdown_is_up_to_date()
    {
        string expected = CoverageReport.Generate(Spec.Endpoints, Spec.Known, Spec.Core);
        string actual = File.ReadAllText(Path.Combine(Spec.Directory, "api-coverage.md")).Replace("\r\n", "\n", StringComparison.Ordinal);

        actual.Should().Be(expected, because: "docs/api-coverage.md is generated; run `dotnet run --project src/tools/Scott.Mail.Smtp2Go.SpecHarvester -- coverage` and commit the result");
    }

    [Fact]
    public void No_operation_is_missing()
    {
        CoverageReport.Rows(Spec.Endpoints, Spec.Known, Spec.Core).Where(r => r.Status == CoverageStatus.Missing).Select(r => r.Operation.Key).Should().BeEmpty(
            because: "every documented operation needs an EndpointTable descriptor or a known-unmodelled.json entry with a reason");
    }

    [Fact]
    public void Every_entry_has_a_reason()
    {
        Spec.Known.Entries.Should().OnlyContain(e => !string.IsNullOrWhiteSpace(e.Reason));
        Spec.Known.Entries.Should().OnlyContain(e => (e.Scope == null) == (e.Field == null), "an entry is either a whole operation (no scope, no field) or one field (scope and field)");
    }

    [Theory]
    [MemberData(nameof(Entries))]
    public void Entry_is_still_needed(string label)
    {
        KnownUnmodelledEntry entry = Spec.Known.Entries.Single(e => e.ToString() == label);
        if (entry.Endpoint == "*")
        {
            return; // global conventions such as subaccount_id
        }

        bool isCallback = entry.Endpoint.StartsWith("callbacks/", StringComparison.Ordinal);
        if (isCallback)
        {
            Spec.Endpoints.Callbacks.Select(c => c.Path).Should().Contain(entry.Endpoint, because: "the entry {0} names a callback pseudo-path that endpoints.json does not have", entry);
        }
        else
        {
            Spec.Endpoints.Operations.Should().Contain(o => o.Path == entry.Endpoint && entry.MatchesMethod(o.Method), because: "the entry {0} is for an operation the docs no longer list; delete it", entry);
        }

        switch (entry.Scope)
        {
            case null when isCallback:
                ModelDiscovery.FindWebhookEventBase(Spec.Core).Should().BeNull(because: "{0} allow-lists the whole callback but WebhookEvent now exists; delete the entry", entry);
                break;
            case null:
                foreach (OperationSummary op in Spec.Endpoints.ForPath(entry.Endpoint).Where(o => entry.MatchesMethod(o.Method)))
                {
                    Spec.Descriptor(op.Path, op.Method).Should().BeNull(because: "{0} is allow-listed as unimplemented but EndpointTable has a descriptor for {1}; delete the entry", entry, op.Key);
                }

                Spec.Models.Where(m => m.Path == entry.Endpoint).Should().BeEmpty(because: "{0} is allow-listed as unimplemented but a model is annotated for it; delete the entry", entry);
                break;
            case KnownUnmodelledScope.Request:
                if (entry.Field != "*")
                {
                    Spec.Endpoints.ForPath(entry.Endpoint).SelectMany(o => o.RequestProperties).Select(p => p.Name).Should().Contain(entry.Field, because: "{0} names a request property the docs no longer list; delete the entry", entry);
                    foreach (AnnotatedModel model in Spec.Models.Where(m => m.Role == ModelRole.Request && m.Path == entry.Endpoint))
                    {
                        ModelDiscovery.WireNames(model.Type).Should().NotContain(entry.Field, because: "{0} is allow-listed as unmodelled but {1} models it; delete the entry", entry, model.Type.Name);
                    }
                }

                break;
            case KnownUnmodelledScope.Response:
                if (entry.Fixture is not null)
                {
                    File.Exists(Fixture.PathOf(entry.Fixture)).Should().BeTrue(because: "{0} names a fixture that does not exist", entry);
                }
                else if (entry.Field != "*")
                {
                    foreach (Type model in Spec.Models.Where(m => m.Path == entry.Endpoint && m.ResponseDataType is not null).Select(m => ModelDiscovery.ElementTypeOrSelf(m.ResponseDataType!)).Where(ModelDiscovery.IsObjectModel))
                    {
                        ModelDiscovery.WireNames(model).Should().NotContain(entry.Field, because: "{0} is allow-listed as unmodelled but {1} models it; delete the entry", entry, model.Name);
                    }
                }

                break;
            case KnownUnmodelledScope.Callback:
                if (entry.Field != "*")
                {
                    Spec.Endpoints.Callbacks.Where(c => c.Path == entry.Endpoint).SelectMany(c => c.Parameters).Select(p => p.Name).Should().Contain(entry.Field, because: "{0} names a callback parameter the docs no longer list; delete the entry", entry);
                    ModelDiscovery.WebhookEventTypes(Spec.Core).SelectMany(ModelDiscovery.WireNames).Should().NotContain(entry.Field, because: "{0} is allow-listed as unmodelled but a WebhookEvent type models it; delete the entry", entry);
                }

                break;
            default:
                throw new InvalidOperationException($"Unknown scope {entry.Scope}.");
        }
    }
}
