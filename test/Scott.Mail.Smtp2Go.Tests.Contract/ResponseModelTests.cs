using System.Reflection;
using Scott.Mail.Smtp2Go.SpecHarvester;
using Scott.Mail.Smtp2Go.Tests.Shared;

namespace Scott.Mail.Smtp2Go.Tests.Contract;

/// <summary>
/// The documented 200 example of every operation with a typed response deserialises through <c>Smtp2GoJsonContext</c> into the response type the
/// client interface returns, with nothing landing in an <c>Extra</c> bag unless allow-listed; and every documented response property is modelled or allow-listed.
/// </summary>
public class ResponseModelTests
{
    public static TheoryData<string> ModelsWithResponses() => Spec.ModelsWithResponses();

    [Fact]
    public void Email_family_response_types_are_discovered_from_the_client_interface()
    {
        Spec.Model(typeof(EmailSendRequest).FullName!).ResponseDataType.Should().Be<EmailSendResult>();
        Spec.Model(typeof(EmailBatchRequest).FullName!).ResponseDataType.Should().Be<IReadOnlyList<EmailBatchItem>>();
        Spec.Model(typeof(ScheduledEmailSearchRequest).FullName!).ResponseDataType.Should().Be<IReadOnlyList<ScheduledEmail>>();
        Spec.Model(typeof(ScheduledEmailRemoveRequest).FullName!).ResponseDataType.Should().BeNull(because: "RemoveScheduledAsync takes a bare id and returns raw JsonElement data");
    }

    [Theory]
    [MemberData(nameof(ModelsWithResponses))]
    public void Documented_example_deserialises_with_nothing_unmodelled(string typeName)
    {
        AnnotatedModel model = Spec.Model(typeName);
        KnownUnmodelledEntry? fixture = Spec.Known.ResponseFixture(model.Path);
        string? json = fixture is not null ? Fixture.Read(fixture.Fixture!) : Spec.Endpoints.ForPath(model.Path).Select(o => o.ResponseExample?.ToJsonString()).FirstOrDefault(e => e is not null);
        if (json is null)
        {
            Assert.Skip($"{model.Path} documents no 200 example and known-unmodelled.json names no fixture for it.");
        }

        object envelope;
        try
        {
            envelope = ContractChecks.DeserializeEnvelope(json, model.ResponseDataType!);
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or NotSupportedException or InvalidOperationException)
        {
            throw new Xunit.Sdk.XunitException($"The documented example of {model.Path} does not deserialise as ApiResponse<{ModelDiscovery.FriendlyName(model.ResponseDataType!)}>: {ex.Message}\n{json}", ex);
        }

        envelope.GetType().GetProperty(nameof(ApiResponse<int>.RequestId))!.GetValue(envelope).Should().BeOfType<string>().Which.Should().NotBeNullOrWhiteSpace();
        ContractChecks.UnexpectedExtraFields(envelope, model.Path, Spec.Known).Should().BeEmpty(because: "every field in the documented example must be modelled or allow-listed under scope 'response'");
    }

    [Theory]
    [MemberData(nameof(ModelsWithResponses))]
    public void Documented_response_properties_are_modelled_or_allow_listed(string typeName)
    {
        AnnotatedModel model = Spec.Model(typeName);

        ContractChecks.UnmodelledResponseProperties(model.ResponseDataType!, model.Path, Spec.Endpoints, Spec.Known).Should().BeEmpty();
    }

    [Fact]
    public void Fixture_entries_point_at_existing_files()
    {
        foreach (KnownUnmodelledEntry entry in Spec.Known.Entries.Where(e => e.Fixture is not null))
        {
            File.Exists(Fixture.PathOf(entry.Fixture!)).Should().BeTrue(because: "{0} names fixture {1}, which must exist under Tests.Shared/Fixtures", entry, entry.Fixture);
        }
    }

    [Fact]
    public void An_unmodelled_field_in_the_example_is_reported()
    {
        // Proves the check bites: a field the model lacks lands in Extra and is reported unless allow-listed.
        const string json = """{"request_id":"r","data":{"succeeded":1,"failed":0,"failures":[],"email_id":"e","brand_new_field":true},"top_level_surprise":1}""";
        object envelope = ContractChecks.DeserializeEnvelope(json, typeof(EmailSendResult));

        IReadOnlyList<string> problems = ContractChecks.UnexpectedExtraFields(envelope, "email/send", Spec.Known);

        problems.Should().HaveCount(2);
        problems.Should().Contain(p => p.Contains("'brand_new_field'", StringComparison.Ordinal) && p.Contains("EmailSendResult", StringComparison.Ordinal));
        problems.Should().Contain(p => p.Contains("'top_level_surprise'", StringComparison.Ordinal));
    }

    [Fact]
    public void Response_types_with_an_extension_bag_expose_it_with_a_setter()
    {
        // Plan 02 deviation 6: the source generator needs a plain setter on [JsonExtensionData] properties.
        foreach (Type type in Spec.Models.Select(m => m.ResponseDataType).Where(t => t is not null).Select(t => ModelDiscovery.ElementTypeOrSelf(t!)).Where(ModelDiscovery.IsObjectModel).Distinct())
        {
            PropertyInfo? bag = ModelDiscovery.ExtensionDataProperty(type);
            bag.Should().NotBeNull(because: "{0} is a response model and must carry a [JsonExtensionData] bag so new server fields are observable", type.Name);
            bag!.SetMethod.Should().NotBeNull();
        }
    }
}
