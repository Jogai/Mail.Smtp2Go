using Scott.Mail.Smtp2Go.Tests.Shared;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Transport;

public class OptionsValidationTests
{
    [Fact]
    public void Missing_api_key_is_an_error()
    {
        Smtp2GoClientOptions options = new();

        options.GetValidationErrors().Should().ContainSingle().Which.Should().Be("ApiKey is required.");
        Action act = options.Validate;
        act.Should().Throw<Smtp2GoValidationException>().Which.Errors.Should().ContainSingle();
    }

    [Fact]
    public void Every_problem_is_listed_at_once()
    {
        Smtp2GoClientOptions options = new()
        {
            BaseUrl = new Uri("ftp://files.example.test/"),
            Timeout = TimeSpan.Zero,
        };

        Action act = options.Validate;

        Smtp2GoValidationException exception = act.Should().Throw<Smtp2GoValidationException>().Which;
        exception.Errors.Should().HaveCount(3);
        exception.Message.Should().Contain("ApiKey is required.").And.Contain("BaseUrl").And.Contain("Timeout");
    }

    [Fact]
    public void Relative_base_url_is_an_error()
    {
        Smtp2GoClientOptions options = new() { ApiKey = TestClient.ApiKey, BaseUrl = new Uri("v3/", UriKind.Relative) };

        options.GetValidationErrors().Should().ContainSingle().Which.Should().Contain("absolute");
    }

    [Fact]
    public void Infinite_timeout_is_allowed()
    {
        Smtp2GoClientOptions options = new() { ApiKey = TestClient.ApiKey, Timeout = Timeout.InfiniteTimeSpan };

        options.GetValidationErrors().Should().BeEmpty();
    }

    [Fact]
    public void Unusual_key_format_is_a_warning_not_an_error()
    {
        Smtp2GoClientOptions options = new() { ApiKey = "sandbox-key" };

        options.GetValidationErrors().Should().BeEmpty();
        options.GetValidationWarnings().Should().ContainSingle().Which.Should().Contain("api-");
        Action act = options.Validate;
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("api-0123456789ABCDEFGHIJKLMNOPQRSTUV", true)]
    [InlineData("api-0123456789abcdefghijklmnopqrstuv", true)]
    [InlineData("api-0123456789ABCDEFGHIJKLMNOPQRSTU", false)]
    [InlineData("api-0123456789ABCDEFGHIJKLMNOPQRSTUVW", false)]
    [InlineData("API-0123456789abcdefghijklmnopqrstuv", false)]
    [InlineData("api-0123456789ABCDEFGHIJKLMNOPQRST-V", false)]
    [InlineData("", false)]
    public void Documented_key_format_is_recognised(string key, bool wellFormed)
    {
        Smtp2GoClientOptions.IsWellFormedApiKey(key).Should().Be(wellFormed);
    }

    [Fact]
    public void Client_constructors_validate()
    {
        Action blankKey = () => _ = new Smtp2GoClient(" ");
        Action nullKey = () => _ = new Smtp2GoClient((string)null!);
        Action badOptions = () => _ = new Smtp2GoClient(new Smtp2GoClientOptions());
        Action nullHttp = () => _ = new Smtp2GoClient((HttpClient)null!, new Smtp2GoClientOptions { ApiKey = TestClient.ApiKey });
        Action nullOptions = () => _ = new Smtp2GoClient(new HttpClient(new FakeHttpMessageHandler()), null!);

        blankKey.Should().Throw<ArgumentException>();
        nullKey.Should().Throw<ArgumentNullException>();
        badOptions.Should().Throw<Smtp2GoValidationException>();
        nullHttp.Should().Throw<ArgumentNullException>();
        nullOptions.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Key_and_options_constructors_share_one_http_client()
    {
        Smtp2GoClient a = new(TestClient.ApiKey);
        Smtp2GoClient b = new(new Smtp2GoClientOptions { ApiKey = TestClient.ApiKey, Region = Region.AU });

        a.Options.ApiKey.Should().Be(TestClient.ApiKey);
        b.Options.Region.Should().Be(Region.AU);
        a.Raw.Should().NotBeSameAs(b.Raw);
    }

    [Fact]
    public void Validation_exception_lists_errors_in_the_message()
    {
        Smtp2GoValidationException exception = new("Nope.", ["first", "second"]);

        exception.Message.Should().Be("Nope. first second");
        exception.Errors.Should().Equal("first", "second");
        new Smtp2GoValidationException().Errors.Should().BeEmpty();
        new Smtp2GoValidationException("m", (Exception?)null).Errors.Should().BeEmpty();
    }
}
