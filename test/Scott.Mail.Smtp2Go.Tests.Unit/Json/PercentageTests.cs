using System.Text.Json;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Json;

public sealed record PercentageEnvelope(Percentage BouncePercent, Percentage? Optional);

public class PercentageTests
{
    private static readonly JsonSerializerOptions s_options = new();

    [Fact]
    public void Reads_a_string_percentage_keeping_the_raw_text()
    {
        Percentage value = JsonSerializer.Deserialize<Percentage>("\"7.33\"", s_options);

        value.Value.Should().Be(7.33m);
        value.Raw.Should().Be("7.33");
    }

    [Fact]
    public void Reads_a_number()
    {
        Percentage value = JsonSerializer.Deserialize<Percentage>("12.5", s_options);

        value.Value.Should().Be(12.5m);
        value.Raw.Should().Be("12.5");
    }

    [Fact]
    public void Keeps_unparseable_text_with_a_null_value()
    {
        Percentage value = JsonSerializer.Deserialize<Percentage>("\"n/a\"", s_options);

        value.Value.Should().BeNull();
        value.Raw.Should().Be("n/a");
    }

    [Fact]
    public void Null_is_the_default_value()
    {
        JsonSerializer.Deserialize<Percentage>("null", s_options).Should().Be(default(Percentage));
        JsonSerializer.Deserialize<Percentage?>("null", s_options).Should().BeNull();
    }

    [Fact]
    public void Writes_the_raw_text_or_the_value()
    {
        JsonSerializer.Serialize(Percentage.FromString("7.33"), s_options).Should().Be("\"7.33\"");
        JsonSerializer.Serialize(new Percentage(0.5m, null), s_options).Should().Be("\"0.5\"");
        JsonSerializer.Serialize(default(Percentage), s_options).Should().Be("null");
    }

    [Fact]
    public void Works_on_properties()
    {
        PercentageEnvelope? envelope = JsonSerializer.Deserialize<PercentageEnvelope>("""{"BouncePercent":"0.00","Optional":null}""", s_options);

        envelope!.BouncePercent.Value.Should().Be(0m);
        envelope.Optional.Should().BeNull();
    }

    [Fact]
    public void Unsupported_tokens_throw()
    {
        Action act = () => JsonSerializer.Deserialize<Percentage>("[1]", s_options);

        act.Should().Throw<JsonException>();
    }
}
