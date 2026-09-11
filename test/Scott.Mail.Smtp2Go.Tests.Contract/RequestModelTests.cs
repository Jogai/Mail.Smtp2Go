using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.SpecHarvester;

namespace Scott.Mail.Smtp2Go.Tests.Contract;

/// <summary>
/// Every request model carrying <c>[Smtp2GoEndpoint]</c> against the documented request properties of its path: nothing on the model that the
/// docs do not list, nothing in the docs that the model (or <c>known-unmodelled.json</c>) does not account for, and documented required fields are <c>required</c> members.
/// </summary>
public class RequestModelTests
{
    public static TheoryData<string> RequestModels() => Spec.ModelNames(ModelRole.Request);

    [Fact]
    public void Email_family_request_models_are_discovered()
    {
        Spec.Models.Where(m => m.Role == ModelRole.Request).Select(m => m.Type).Should().Contain(
            [typeof(EmailSendRequest), typeof(EmailMimeRequest), typeof(EmailBatchRequest), typeof(ScheduledEmailSearchRequest), typeof(ScheduledEmailRemoveRequest)],
            because: "the email family annotates its request records and discovery must find them");
    }

    [Theory]
    [MemberData(nameof(RequestModels))]
    public void Attribute_path_is_documented(string typeName)
    {
        AnnotatedModel model = Spec.Model(typeName);

        Spec.Endpoints.ForPath(model.Path).Should().NotBeEmpty(because: "{0} is annotated with [Smtp2GoEndpoint(\"{1}\")] but the docs list no such path (typo?)", model.Type.Name, model.Path);
    }

    [Theory]
    [MemberData(nameof(RequestModels))]
    public void Every_model_property_is_a_documented_request_property(string typeName)
    {
        AnnotatedModel model = Spec.Model(typeName);

        ContractChecks.UndocumentedRequestProperties(model.Type, model.Path, Spec.Endpoints).Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(RequestModels))]
    public void Every_documented_request_property_is_modelled_or_allow_listed(string typeName)
    {
        AnnotatedModel model = Spec.Model(typeName);

        ContractChecks.UnmodelledRequestProperties(model.Type, model.Path, Spec.Endpoints, Spec.Known).Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(RequestModels))]
    public void Documented_required_properties_are_required_members(string typeName)
    {
        AnnotatedModel model = Spec.Model(typeName);

        ContractChecks.OptionalRequiredProperties(model.Type, model.Path, Spec.Endpoints, Spec.Known).Should().BeEmpty();
    }

    [Fact]
    public void A_wrong_wire_name_is_reported()
    {
        // Proves the check bites: a model with `mime_mail` instead of `mime_email` must be caught, in both directions.
        ContractChecks.UndocumentedRequestProperties(typeof(MisspelledMimeRequest), "email/mime", Spec.Endpoints)
            .Should().ContainSingle().Which.Should().Contain("MisspelledMimeRequest.mime_mail is not a documented request property of email/mime");
        ContractChecks.UnmodelledRequestProperties(typeof(MisspelledMimeRequest), "email/mime", Spec.Endpoints, Spec.Known)
            .Should().ContainSingle().Which.Should().Contain("'mime_email'");
        ContractChecks.OptionalRequiredProperties(typeof(OptionalMimeRequest), "email/mime", Spec.Endpoints, Spec.Known)
            .Should().ContainSingle().Which.Should().Contain("OptionalMimeRequest.MimeEmail is not a required member");
    }

    [Fact]
    public void Properties_without_an_explicit_wire_name_use_snake_case()
    {
        // The serializer context applies SnakeCaseLower, so a property without [JsonPropertyName] is compared under that name.
        ModelDiscovery.WireNames(typeof(OptionalMimeRequest)).Should().Equal("mime_email", "fast_accept");
    }

    private sealed record MisspelledMimeRequest
    {
        [JsonPropertyName("mime_mail")]
        public required string MimeEmail { get; init; }

        [JsonPropertyName("schedule")]
        public DateTimeOffset? Schedule { get; init; }

        [JsonPropertyName("fastaccept")]
        public bool? FastAccept { get; init; }
    }

    private sealed record OptionalMimeRequest
    {
        public string? MimeEmail { get; init; }

        public bool? FastAccept { get; init; }

        [JsonIgnore]
        public string? Ignored { get; init; }
    }
}
