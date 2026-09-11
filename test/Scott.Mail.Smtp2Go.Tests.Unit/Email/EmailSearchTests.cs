using System.Net;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Email;

#pragma warning disable CS0618 // Tests the deprecated endpoint deliberately.
public class EmailSearchTests
{
    [Fact]
    public void Request_serialises_every_documented_field()
    {
        EmailSearchRequest request = new()
        {
            StartDate = new DateTimeOffset(2016, 6, 27, 0, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2016, 6, 28, 0, 0, 0, TimeSpan.Zero),
            Limit = 50,
            StatusCounts = true,
            OpenedOnly = false,
            ClickedOnly = false,
            IgnoreCase = true,
            FilterQuery = "status:delivered",
            EmailId = ["1u0SwL-B9zBpi9ffUq-JAB2"],
            Username = "smtpuser@example.com",
            Headers = ["X-Campaign"],
            ContinueToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9",
        };

        Golden.AssertMatches(JsonSerializer.Serialize(request, Smtp2GoJsonContext.Default.EmailSearchRequest), "search-request.json");
    }

    [Fact]
    public void Docs_response_deserialises_with_raw_emails()
    {
        ApiResponse<EmailSearchResult> response = JsonSerializer.Deserialize(Fixture.Read("Email/search-response.json"), Smtp2GoJsonContext.Default.ApiResponseEmailSearchResult)!;

        response.Data.Count.Should().Be(1);
        response.Data.ContinueToken.Should().StartWith("eyJ");
        JsonElement email = response.Data.Emails.Should().ContainSingle().Which;
        email.GetProperty("email_id").GetString().Should().Be("1u0SwL-B9zBpi9ffUq-JAB2");
        email.GetProperty("opens").GetArrayLength().Should().Be(1);
        response.Data.Extra.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_posts_to_email_search_with_its_rate_limit_class()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("email/search", HttpStatusCode.OK, Fixture.Read("Email/search-response.json"));
        Smtp2GoClient client = TestClient.Create(handler);

        ApiResponse<EmailSearchResult> response = await client.Email.SearchAsync(new EmailSearchRequest { Limit = 10 });

        handler.LastRequest.Endpoint!.Path.Should().Be("email/search");
        handler.LastRequest.Endpoint.RateLimit.Should().Be(RateLimitClass.EmailSearch);
        handler.LastRequest.Body.Should().Be("""{"limit":10}""");
        response.Data.Count.Should().Be(1);
    }

    [Fact]
    public void Types_and_method_are_marked_obsolete()
    {
        typeof(EmailSearchRequest).GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false).Should().ContainSingle();
        typeof(EmailSearchResult).GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false).Should().ContainSingle();
        typeof(IEmailClient).GetMethod(nameof(IEmailClient.SearchAsync))!.GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false)
            .Should().ContainSingle().Which.As<ObsoleteAttribute>().Message.Should().Contain("Activity.SearchAsync");
    }
}
#pragma warning restore CS0618
