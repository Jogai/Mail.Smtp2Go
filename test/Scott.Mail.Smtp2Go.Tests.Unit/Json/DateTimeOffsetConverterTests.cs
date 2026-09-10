using System.Globalization;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Json.Converters;
using Scott.Mail.Smtp2Go.Tests.Shared;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Json;

public class DateTimeOffsetConverterTests
{
    private static readonly JsonSerializerOptions s_options = new() { Converters = { new Smtp2GoDateTimeOffsetConverter() } };

    public static TheoryData<string, string, string> ObservedFormats()
    {
        TheoryData<string, string, string> data = [];
        using JsonDocument document = JsonDocument.Parse(Fixture.Read("Json/timestamps.json"));
        foreach (JsonElement row in document.RootElement.EnumerateArray())
        {
            data.Add(row.GetProperty("source").GetString()!, row.GetProperty("input").GetString()!, row.GetProperty("expectedUtc").GetString()!);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ObservedFormats))]
    public void Every_observed_format_parses_to_the_expected_instant(string source, string input, string expectedUtc)
    {
        DateTimeOffset expected = DateTimeOffset.Parse(expectedUtc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

        DateTimeOffset parsed = JsonSerializer.Deserialize<DateTimeOffset>($"\"{input}\"", s_options);

        parsed.UtcDateTime.Should().Be(expected.UtcDateTime, because: source);
    }

    [Fact]
    public void Offsets_are_preserved_not_normalised()
    {
        DateTimeOffset parsed = JsonSerializer.Deserialize<DateTimeOffset>("\"2025-09-10 13:15:00 +1200\"", s_options);

        parsed.Offset.Should().Be(TimeSpan.FromHours(12));
        parsed.Hour.Should().Be(13);
    }

    [Fact]
    public void Writes_iso8601_utc_with_whole_seconds()
    {
        DateTimeOffset value = new(2025, 9, 10, 13, 15, 42, 500, TimeSpan.FromHours(12));

        string json = JsonSerializer.Serialize(value, s_options);

        json.Should().Be("\"2025-09-10T01:15:42Z\"");
        Smtp2GoDateTimeOffsetConverter.Format(value).Should().Be("2025-09-10T01:15:42Z");
    }

    [Fact]
    public void Nullable_round_trips_null()
    {
        JsonSerializer.Deserialize<DateTimeOffset?>("null", s_options).Should().BeNull();
        JsonSerializer.Serialize<DateTimeOffset?>(null, s_options).Should().Be("null");
    }

    [Theory]
    [InlineData("\"not a date\"")]
    [InlineData("\"2024-13-45\"")]
    [InlineData("1700000000")]
    [InlineData("true")]
    public void Unrecognised_input_throws_JsonException(string json)
    {
        Action act = () => JsonSerializer.Deserialize<DateTimeOffset>(json, s_options);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void TryParse_is_usable_directly()
    {
        Smtp2GoDateTimeOffsetConverter.TryParse("2022-11-01 00:00:00+00:00", out DateTimeOffset value).Should().BeTrue();
        value.Should().Be(new DateTimeOffset(2022, 11, 1, 0, 0, 0, TimeSpan.Zero));
        Smtp2GoDateTimeOffsetConverter.TryParse("garbage", out _).Should().BeFalse();
    }
}
