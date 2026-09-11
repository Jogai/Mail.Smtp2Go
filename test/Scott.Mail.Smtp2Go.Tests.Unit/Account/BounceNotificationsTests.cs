using System.Text.Json;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Account;

public class BounceNotificationsTests
{
    private static readonly JsonSerializerOptions s_options = new() { Converters = { new BounceNotificationsConverter() } };

    [Fact]
    public void Singletons_carry_the_documented_wire_values()
    {
        BounceNotifications.From.Kind.Should().Be(BounceNotificationsKind.From);
        BounceNotifications.From.Value.Should().Be("from");
        BounceNotifications.From.EmailAddress.Should().BeNull();
        BounceNotifications.Drop.Kind.Should().Be(BounceNotificationsKind.Drop);
        BounceNotifications.Drop.ToString().Should().Be("drop");
    }

    [Fact]
    public void Email_validates_and_trims_the_address()
    {
        BounceNotifications email = BounceNotifications.Email(" bounces@example.com ");

        email.Kind.Should().Be(BounceNotificationsKind.Email);
        email.Value.Should().Be("bounces@example.com");
        email.EmailAddress.Should().Be("bounces@example.com");
        ((Action)(() => BounceNotifications.Email("not-an-address"))).Should().Throw<ArgumentException>();
        ((Action)(() => BounceNotifications.Email(" "))).Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("from", BounceNotificationsKind.From)]
    [InlineData("FROM", BounceNotificationsKind.From)]
    [InlineData("drop", BounceNotificationsKind.Drop)]
    [InlineData("someone@example.com", BounceNotificationsKind.Email)]
    [InlineData("whatever", BounceNotificationsKind.Email)]
    public void Parse_maps_the_keywords_and_keeps_anything_else_as_an_address(string wire, BounceNotificationsKind kind)
    {
        BounceNotifications parsed = BounceNotifications.Parse(wire);

        parsed.Kind.Should().Be(kind);
        if (kind == BounceNotificationsKind.Email)
        {
            parsed.Value.Should().Be(wire);
        }
    }

    [Fact]
    public void Equality_is_by_kind_and_value()
    {
        BounceNotifications.Parse("from").Should().Be(BounceNotifications.From);
        BounceNotifications.Email("a@b.c").Should().Be(BounceNotifications.Parse("a@b.c"));
        BounceNotifications.Email("a@b.c").Should().NotBe(BounceNotifications.Email("x@b.c"));
    }

    [Fact]
    public void Converter_round_trips_the_wire_string()
    {
        JsonSerializer.Serialize(BounceNotifications.Drop, s_options).Should().Be("\"drop\"");
        JsonSerializer.Serialize(BounceNotifications.Email("a@b.c"), s_options).Should().Be("\"a@b.c\"");
        JsonSerializer.Deserialize<BounceNotifications>("\"from\"", s_options).Should().Be(BounceNotifications.From);
        JsonSerializer.Deserialize<BounceNotifications>("null", s_options).Should().BeNull();
        ((Action)(() => JsonSerializer.Deserialize<BounceNotifications>("1", s_options))).Should().Throw<JsonException>();
    }
}
