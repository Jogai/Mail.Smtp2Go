using System.Text.Json;
using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Json;

public class NumberOrStringConverterTests
{
    private static readonly JsonSerializerOptions s_options = new() { Converters = { new NumberOrStringConverter() } };

    [Theory]
    [InlineData("\"+15185550120\"", "+15185550120")]
    [InlineData("15185550120", "15185550120")]
    [InlineData("12.5", "12.5")]
    [InlineData("true", "true")]
    [InlineData("null", null)]
    public void Reads_strings_numbers_and_booleans_as_text(string json, string? expected)
    {
        JsonSerializer.Deserialize<Holder>($$"""{"value":{{json}}}""", s_options)!.Value.Should().Be(expected);
    }

    [Fact]
    public void Writes_a_string_and_rejects_other_tokens()
    {
        JsonSerializer.Serialize(new Holder { Value = "15185550120" }, s_options).Should().Be("""{"value":"15185550120"}""");
        ((Action)(() => JsonSerializer.Deserialize<Holder>("""{"value":[1]}""", s_options))).Should().Throw<JsonException>();
    }

    private sealed record Holder
    {
        [JsonPropertyName("value")]
        [JsonConverter(typeof(NumberOrStringConverter))]
        public string? Value { get; init; }
    }
}
