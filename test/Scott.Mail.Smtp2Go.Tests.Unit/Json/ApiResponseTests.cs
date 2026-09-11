using System.Text.Json;
using System.Text.Json.Nodes;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Json;

public class ApiResponseTests
{
    [Fact]
    public void Extra_captures_unknown_top_level_fields()
    {
        ApiResponse<JsonElement>? response = JsonSerializer.Deserialize(Fixture.Read("Transport/success-with-extra.json"), Smtp2GoJsonContext.Default.ApiResponseJsonElement);

        response.Should().NotBeNull();
        response!.RequestId.Should().Be("6c7d8e9f-0a1b-2c3d-4e5f-6a7b8c9d0e1f");
        response.Data.GetProperty("succeeded").GetInt32().Should().Be(1);
        response.Extra.Should().NotBeNull();
        response.Extra!.Keys.Should().BeEquivalentTo("warnings", "meta");
        response.Extra["meta"].GetProperty("region").GetString().Should().Be("eu");
    }

    [Fact]
    public void Extra_is_null_when_nothing_is_unknown()
    {
        ApiResponse<JsonElement>? response = JsonSerializer.Deserialize("""{"request_id":"r","data":{}}""", Smtp2GoJsonContext.Default.ApiResponseJsonElement);

        response!.Extra.Should().BeNull();
    }

    [Fact]
    public void Property_names_are_case_insensitive_and_comments_are_skipped()
    {
        ApiResponse<JsonObject>? response = JsonSerializer.Deserialize("""{/* c */"Request_Id":"r","DATA":{"a":1}}""", Smtp2GoJsonContext.Default.ApiResponseJsonObject);

        response!.RequestId.Should().Be("r");
        response.Data["a"]!.GetValue<int>().Should().Be(1);
    }

    [Fact]
    public void Missing_required_members_throw()
    {
        Action noRequestId = () => JsonSerializer.Deserialize("""{"data":{}}""", Smtp2GoJsonContext.Default.ApiResponseJsonElement);
        Action noData = () => JsonSerializer.Deserialize("""{"request_id":"r"}""", Smtp2GoJsonContext.Default.ApiResponseJsonElement);

        noRequestId.Should().Throw<JsonException>();
        noData.Should().Throw<JsonException>();
    }

    [Fact]
    public void Serialisation_uses_snake_case_and_omits_nulls()
    {
        ApiResponse<JsonElement> response = new() { RequestId = "r", Data = TestClient.Json("{}") };

        string json = JsonSerializer.Serialize(response, Smtp2GoJsonContext.Default.ApiResponseJsonElement);

        json.Should().Be("""{"request_id":"r","data":{}}""");
    }

    [Fact]
    public void Aot_smoke_the_context_resolves_the_envelope_and_raw_types()
    {
        Smtp2GoJsonContext.Default.GetTypeInfo(typeof(ApiResponse<JsonElement>)).Should().NotBeNull();
        Smtp2GoJsonContext.Default.GetTypeInfo(typeof(ApiResponse<JsonNode>)).Should().NotBeNull();
        Smtp2GoJsonContext.Default.GetTypeInfo(typeof(ApiResponse<JsonObject>)).Should().NotBeNull();
        Smtp2GoJsonContext.Default.GetTypeInfo(typeof(ApiResponse<JsonArray>)).Should().NotBeNull();
        Smtp2GoJsonContext.Default.GetTypeInfo(typeof(JsonElement)).Should().NotBeNull();
        Smtp2GoJsonContext.Default.GetTypeInfo(typeof(ApiResponse<Guid>)).Should().BeNull();
        Smtp2GoJsonContext.Default.Options.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.SnakeCaseLower);
        Smtp2GoJsonContext.Default.Options.NumberHandling.Should().Be(System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString);
        Smtp2GoJsonContext.Default.Options.Converters.Should().ContainSingle(c => c is Scott.Mail.Smtp2Go.Json.Converters.Smtp2GoDateTimeOffsetConverter);
    }
}
