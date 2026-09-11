using System.Text.Json;
using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Email;

public class EmailAddressTests
{
    [Theory]
    [InlineData("alice@example.com", "alice@example.com", null)]
    [InlineData("  alice@example.com  ", "alice@example.com", null)]
    [InlineData("<alice@example.com>", "alice@example.com", null)]
    [InlineData("Alice <alice@example.com>", "alice@example.com", "Alice")]
    [InlineData("Alice Smith <alice@example.com>", "alice@example.com", "Alice Smith")]
    [InlineData("\"Smith, Alice\" <alice@example.com>", "alice@example.com", "Smith, Alice")]
    [InlineData("\"Alice \\\"Al\\\" Smith\" <alice@example.com>", "alice@example.com", "Alice \"Al\" Smith")]
    [InlineData("Zoë Müller <zoe@example.com>", "zoe@example.com", "Zoë Müller")]
    [InlineData("山田太郎 <taro@example.jp>", "taro@example.jp", "山田太郎")]
    [InlineData("  Alice   <  alice@example.com > ", "alice@example.com", "Alice")]
    public void Parses_bare_and_named_forms(string text, string address, string? name)
    {
        EmailAddress parsed = EmailAddress.Parse(text);

        parsed.Address.Should().Be(address);
        parsed.Name.Should().Be(name);
    }

    [Theory]
    [InlineData("alice@example.com", null, "alice@example.com")]
    [InlineData("alice@example.com", "Alice", "Alice <alice@example.com>")]
    [InlineData("alice@example.com", "Alice Smith", "Alice Smith <alice@example.com>")]
    [InlineData("alice@example.com", "Smith, Alice", "\"Smith, Alice\" <alice@example.com>")]
    [InlineData("alice@example.com", "Alice <admin>", "\"Alice <admin>\" <alice@example.com>")]
    [InlineData("alice@example.com", "Alice \"Al\" Smith", "\"Alice \\\"Al\\\" Smith\" <alice@example.com>")]
    [InlineData("zoe@example.com", "Zoë Müller", "Zoë Müller <zoe@example.com>")]
    [InlineData("alice@example.com", "  ", "alice@example.com")]
    public void Formats_and_quotes_when_needed(string address, string? name, string expected)
    {
        new EmailAddress(address, name).ToString().Should().Be(expected);
    }

    [Theory]
    [InlineData("Alice <alice@example.com>")]
    [InlineData("\"Smith, Alice\" <alice@example.com>")]
    [InlineData("\"A \\\"quoted\\\" \\\\ name\" <alice@example.com>")]
    [InlineData("alice@example.com")]
    public void Formatting_round_trips(string text)
    {
        EmailAddress.Parse(text).ToString().Should().Be(text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Alice alice@example.com>")]
    [InlineData("Alice <alice example.com>")]
    [InlineData("<>")]
    [InlineData("a<b>@example.com")]
    public void Rejects_malformed_input(string text)
    {
        EmailAddress.TryParse(text, out _).Should().BeFalse();
        Action act = () => EmailAddress.Parse(text);
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Constructor_rejects_addresses_with_whitespace_or_brackets()
    {
        Func<EmailAddress> whitespace = () => new EmailAddress("alice example.com");
        Func<EmailAddress> brackets = () => new EmailAddress("<alice@example.com>");
        Func<EmailAddress> empty = () => new EmailAddress(" ");

        whitespace.Should().Throw<ArgumentException>();
        brackets.Should().Throw<ArgumentException>();
        empty.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Implicit_conversion_from_string_parses()
    {
        EmailAddress address = "Alice <alice@example.com>";

        address.Should().Be(new EmailAddress("alice@example.com", "Alice"));
        EmailAddress.FromString("bob@example.com").Address.Should().Be("bob@example.com");
    }

    [Fact]
    public void Equality_ignores_case_of_the_address_and_ignores_the_name()
    {
        EmailAddress a = new("Alice@Example.com", "Alice");
        EmailAddress b = new("alice@example.com", "Someone Else");
        EmailAddress c = new("bob@example.com");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(c);
        (a != c).Should().BeTrue();
    }

    [Fact]
    public void Default_value_is_empty_and_formats_as_empty_string()
    {
        EmailAddress empty = default;

        empty.IsEmpty.Should().BeTrue();
        empty.ToString().Should().BeEmpty();
        empty.GetHashCode().Should().Be(0);
        new EmailAddress("a@b.c").IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Converter_writes_the_wire_form_and_reads_it_back()
    {
        JsonSerializerOptions options = new() { TypeInfoResolver = AddressJsonContext.Default };
        AddressHolder holder = new() { Address = new EmailAddress("alice@example.com", "Smith, Alice") };

        string json = JsonSerializer.Serialize(holder, options);
        AddressHolder? back = JsonSerializer.Deserialize<AddressHolder>(json, options);

        using JsonDocument document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("Address").GetString().Should().Be("\"Smith, Alice\" <alice@example.com>");
        back!.Address.Should().Be(holder.Address);
        back.Address.Name.Should().Be("Smith, Alice");
    }

    [Theory]
    [InlineData("""{"Address":42}""")]
    [InlineData("""{"Address":"not an address <"}""")]
    public void Converter_rejects_non_strings_and_malformed_addresses(string json)
    {
        JsonSerializerOptions options = new() { TypeInfoResolver = AddressJsonContext.Default };

        Action act = () => JsonSerializer.Deserialize<AddressHolder>(json, options);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Converter_is_attached_by_attribute()
    {
        typeof(EmailAddress).GetCustomAttributes(typeof(JsonConverterAttribute), inherit: false)
            .Should().ContainSingle().Which.As<JsonConverterAttribute>().ConverterType.Should().Be<EmailAddressConverter>();
    }
}

public sealed class AddressHolder
{
    public EmailAddress Address { get; set; }
}

[JsonSerializable(typeof(AddressHolder))]
public sealed partial class AddressJsonContext : JsonSerializerContext
{
}
