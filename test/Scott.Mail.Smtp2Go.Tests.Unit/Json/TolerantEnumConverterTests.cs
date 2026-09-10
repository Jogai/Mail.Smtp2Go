using System.Text.Json;
using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Json;

[JsonConverter(typeof(TolerantEnumConverter<ProbeEvent>))]
public enum ProbeEvent
{
    Unknown = 0,
    Delivered = 1,
    [JsonStringEnumMemberName("spam_complaint")]
    SpamComplaint = 2,
    [JsonStringEnumMemberName("output-format")]
    OutputFormat = 3,
}

[JsonConverter(typeof(TolerantEnumConverter<StrictProbe>))]
public enum StrictProbe
{
    Alpha = 1,
    Beta = 2,
}

public sealed record ProbeEnvelope(ProbeEvent Event, ProbeEvent? Optional);

public class TolerantEnumConverterTests
{
    private static readonly JsonSerializerOptions s_options = new();

    [Theory]
    [InlineData("\"delivered\"", ProbeEvent.Delivered)]
    [InlineData("\"Delivered\"", ProbeEvent.Delivered)]
    [InlineData("\"DELIVERED\"", ProbeEvent.Delivered)]
    [InlineData("\"spam_complaint\"", ProbeEvent.SpamComplaint)]
    [InlineData("\"SPAM_COMPLAINT\"", ProbeEvent.SpamComplaint)]
    [InlineData("\"SpamComplaint\"", ProbeEvent.SpamComplaint)]
    [InlineData("\"spam-complaint\"", ProbeEvent.SpamComplaint)]
    [InlineData("\"spam complaint\"", ProbeEvent.SpamComplaint)]
    [InlineData("\"output-format\"", ProbeEvent.OutputFormat)]
    [InlineData("\"output_format\"", ProbeEvent.OutputFormat)]
    [InlineData("\" delivered \"", ProbeEvent.Delivered)]
    [InlineData("2", ProbeEvent.SpamComplaint)]
    [InlineData("\"resubscribe\"", ProbeEvent.Unknown)]
    [InlineData("\"\"", ProbeEvent.Unknown)]
    public void Reads_tolerantly(string json, ProbeEvent expected)
    {
        JsonSerializer.Deserialize<ProbeEvent>(json, s_options).Should().Be(expected);
    }

    [Fact]
    public void Unknown_value_without_an_Unknown_member_throws()
    {
        Action act = () => JsonSerializer.Deserialize<StrictProbe>("\"gamma\"", s_options);

        act.Should().Throw<JsonException>().WithMessage("*gamma*StrictProbe*");
    }

    [Theory]
    [InlineData(ProbeEvent.Delivered, "\"delivered\"")]
    [InlineData(ProbeEvent.SpamComplaint, "\"spam_complaint\"")]
    [InlineData(ProbeEvent.OutputFormat, "\"output-format\"")]
    public void Writes_the_wire_name(ProbeEvent value, string expected)
    {
        JsonSerializer.Serialize(value, s_options).Should().Be(expected);
    }

    [Fact]
    public void Writing_Unknown_throws_so_it_never_reaches_the_api()
    {
        Action act = () => JsonSerializer.Serialize(ProbeEvent.Unknown, s_options);

        act.Should().Throw<JsonException>().WithMessage("*Unknown*");
    }

    [Fact]
    public void Works_on_properties_including_nullable_ones()
    {
        ProbeEnvelope? envelope = JsonSerializer.Deserialize<ProbeEnvelope>("""{"Event":"spam_complaint","Optional":null}""", s_options);

        envelope.Should().Be(new ProbeEnvelope(ProbeEvent.SpamComplaint, null));
        JsonSerializer.Serialize(new ProbeEnvelope(ProbeEvent.Delivered, ProbeEvent.SpamComplaint), s_options)
            .Should().Be("""{"Event":"delivered","Optional":"spam_complaint"}""");
    }

    [Fact]
    public void Non_string_non_number_tokens_throw()
    {
        Action act = () => JsonSerializer.Deserialize<ProbeEvent>("true", s_options);

        act.Should().Throw<JsonException>();
    }
}
