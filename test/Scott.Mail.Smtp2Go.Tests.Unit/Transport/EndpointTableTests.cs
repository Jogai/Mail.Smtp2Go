using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Transport;

public class EndpointTableTests
{
    [Fact]
    public void Known_path_returns_its_descriptor()
    {
        Endpoint endpoint = EndpointTable.Get("email/search");

        endpoint.Should().Be(new Endpoint("email/search", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: false, RateLimitClass.EmailSearch, Endpoint.DefaultMaxBodyBytes));
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
        IEnumerable<Endpoint> email = EndpointTable.All.Where(e => e.Path.StartsWith("email/", StringComparison.Ordinal));
        email.Select(e => e.Path).Should().BeEquivalentTo(
            "email/send", "email/mime", "email/batch", "email/search", "email/scheduled/search", "email/scheduled/remove");
        email.Where(e => e.Path is "email/send" or "email/mime" or "email/batch").Should().OnlyContain(e => e.MaxBodyBytes == Endpoint.EmailMaxBodyBytes && !e.Idempotent);
        email.Where(e => e.Path is "email/search" or "email/scheduled/search" or "email/scheduled/remove").Should().OnlyContain(e => e.MaxBodyBytes == Endpoint.DefaultMaxBodyBytes);
        email.Where(e => e.Path is "email/search" or "email/scheduled/search").Should().OnlyContain(e => e.Idempotent);
        EndpointTable.Get("email/scheduled/remove").Idempotent.Should().BeFalse();
        email.Should().OnlyContain(e => e.Method == HttpMethod.Post && !e.AcceptsSubaccountId);
        EndpointTable.Get("email/search").RateLimit.Should().Be(RateLimitClass.EmailSearch);
        email.Where(e => e.Path != "email/search").Should().OnlyContain(e => e.RateLimit == RateLimitClass.None);
    }

    [Fact]
    public void Every_endpoint_outside_email_has_the_default_body_limit_and_posts()
    {
        EndpointTable.All.Where(e => !e.Path.StartsWith("email/", StringComparison.Ordinal)).Should().OnlyContain(e => e.MaxBodyBytes == Endpoint.DefaultMaxBodyBytes && e.Method == HttpMethod.Post);
    }

    [Fact]
    public void Webhook_family_is_seeded()
    {
        IEnumerable<Endpoint> webhooks = EndpointTable.All.Where(e => e.Path.StartsWith("webhook/", StringComparison.Ordinal));
        webhooks.Select(e => e.Path).Should().BeEquivalentTo("webhook/view", "webhook/add", "webhook/edit", "webhook/remove");
        webhooks.Should().OnlyContain(e => e.AcceptsSubaccountId && e.RateLimit == RateLimitClass.None);
        webhooks.Where(e => e.Idempotent).Select(e => e.Path).Should().Equal("webhook/view");
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
