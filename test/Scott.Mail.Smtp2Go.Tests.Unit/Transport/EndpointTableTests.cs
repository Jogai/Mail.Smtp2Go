using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Transport;

public class EndpointTableTests
{
    [Fact]
    public void Known_path_returns_its_descriptor()
    {
        Endpoint endpoint = EndpointTable.Get("email/search");

        endpoint.Should().Be(new Endpoint("email/search", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: false, RateLimitClass.EmailSearch, Endpoint.EmailMaxBodyBytes));
    }

    [Theory]
    [InlineData("/email/send")]
    [InlineData(" email/send ")]
    public void Leading_slash_and_whitespace_are_normalised(string path)
    {
        EndpointTable.TryGet(path, out Endpoint? endpoint).Should().BeTrue();
        endpoint!.Path.Should().Be("email/send");
    }

    [Fact]
    public void Unknown_path_gets_a_conservative_default()
    {
        Endpoint endpoint = EndpointTable.Get("/something/new");

        EndpointTable.TryGet("something/new", out _).Should().BeFalse();
        endpoint.Path.Should().Be("something/new");
        endpoint.Method.Should().Be(HttpMethod.Post);
        endpoint.Idempotent.Should().BeFalse();
        endpoint.AcceptsSubaccountId.Should().BeFalse();
        endpoint.RateLimit.Should().Be(RateLimitClass.None);
        endpoint.MaxBodyBytes.Should().Be(Endpoint.DefaultMaxBodyBytes);
    }

    [Fact]
    public void Email_family_is_seeded()
    {
        EndpointTable.All.Select(e => e.Path).Should().BeEquivalentTo(
            "email/send", "email/mime", "email/batch", "email/search", "email/scheduled/search", "email/scheduled/remove");
        EndpointTable.All.Should().OnlyContain(e => e.MaxBodyBytes == Endpoint.EmailMaxBodyBytes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Blank_paths_are_rejected(string path)
    {
        Action act = () => EndpointTable.Get(path);

        act.Should().Throw<ArgumentException>();
    }
}
