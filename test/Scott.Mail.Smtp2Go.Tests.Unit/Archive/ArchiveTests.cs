using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Tests.Unit.Email;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Archive;

public class ArchiveTests
{
    private const string DownloadUrl = "https://api.smtp2go.com/archive-attachment/abc123";

    [Fact]
    public void Archive_family_is_seeded()
    {
        IEnumerable<Endpoint> family = EndpointTable.All.Where(e => e.Path.StartsWith("archive/", StringComparison.Ordinal));

        family.Select(e => e.Path).Should().BeEquivalentTo("archive/search", "archive/email");
        family.Should().OnlyContain(e => e.Idempotent && !e.AcceptsSubaccountId && e.RateLimit == RateLimitClass.None);
    }

    [Fact]
    public void Search_request_serialises_every_filter()
    {
        ArchiveSearchRequest request = new()
        {
            StartDate = new DateTimeOffset(2026, 9, 11, 0, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2026, 9, 12, 0, 0, 0, TimeSpan.Zero),
            Limit = 5000,
            Username = "api-12345678",
            Recipient = "test@test.com",
            Sender = "alice@example.com",
            EnvelopeFrom = "bounce@example.com",
            Subject = "Scott.Mail.Smtp2Go demo",
            Headers = "X-Campaign: spring",
            ContinueToken = "eyJwYWdlIjoyfQ==",
        };

        Golden.AssertMatchesFixture(JsonSerializer.Serialize(request, Smtp2GoJsonContext.Default.ArchiveSearchRequest), "Archive/search-request-full.json");
        JsonSerializer.Serialize(new ArchiveSearchRequest(), Smtp2GoJsonContext.Default.ArchiveSearchRequest).Should().Be("{}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5001)]
    public void Limit_is_validated(int limit)
    {
        List<string> errors = [];

        ((IRequestValidator)new ArchiveSearchRequest { Limit = limit }).Validate(EndpointTable.Get("archive/search"), errors);

        errors.Should().Equal("limit must be between 1 and 5000.");
    }

    [Fact]
    public void Docs_search_response_deserialises_with_an_empty_extra()
    {
        ApiResponse<ArchiveSearchResult> response = JsonSerializer.Deserialize(Fixture.Read("Archive/search-response.json"), Smtp2GoJsonContext.Default.ApiResponseArchiveSearchResult)!;

        response.Data.EmailCount.Should().Be(1);
        response.Data.ContinueToken.Should().BeNull();
        ArchivedEmail email = response.Data.Emails.Should().ContainSingle().Which;
        email.AttachmentCount.Should().Be(0);
        email.Attachments.Should().BeEmpty();
        email.ByteCount.Should().Be(1422);
        email.EmailId.Should().Be("1u0SwL-B9zBpi9ffUq-JAB2");
        email.EnvelopeFrom.Should().Be("test@test.com");
        email.Headers.Should().Be("...");
        email.Recipient.Should().Be("test@test.com");
        email.Sender.Should().Be("test@test.com");
        email.Sent.Should().Be(new DateTimeOffset(2021, 11, 8, 18, 58, 47, TimeSpan.Zero));
        email.Subject.Should().Be("test");
        email.To.Should().Be("test@test.com");
        email.Url.Should().Be("...");
        email.Username.Should().Be("api-12345678");
        email.Extra.Should().BeNull();
        response.Data.Extra.Should().BeNull();
    }

    [Fact]
    public void Docs_email_response_deserialises_with_an_empty_extra()
    {
        ApiResponse<ArchivedEmail> response = JsonSerializer.Deserialize(Fixture.Read("Archive/email-response.json"), Smtp2GoJsonContext.Default.ApiResponseArchivedEmail)!;

        response.Data.ByteCount.Should().Be(1428);
        response.Data.Sent.Should().Be(new DateTimeOffset(2021, 10, 19, 21, 35, 40, TimeSpan.Zero));
        response.Data.Extra.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_and_GetAsync_post_to_the_documented_paths()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler()
            .Respond("archive/search", HttpStatusCode.OK, Fixture.Read("Archive/search-response.json"))
            .Respond("archive/email", HttpStatusCode.OK, Fixture.Read("Archive/email-response.json"));
        Smtp2GoClient client = TestClient.Create(handler);

        ApiResponse<ArchiveSearchResult> search = await client.Archive.SearchAsync(new ArchiveSearchRequest { Subject = "test" });
        ApiResponse<ArchivedEmail> email = await client.Archive.GetAsync("1u0SwL-B9zBpi9ffUq-JAB2");

        handler.Requests[0].Endpoint!.Path.Should().Be("archive/search");
        handler.Requests[0].Body.Should().Be("""{"subject":"test"}""");
        handler.Requests[1].Endpoint!.Path.Should().Be("archive/email");
        Golden.AssertMatchesFixture(handler.Requests[1].Body!, "Archive/email-request.json");
        search.Data.EmailCount.Should().Be(1);
        email.Data.EmailId.Should().Be("1u0SwL-B9zBpi9ffUq-JAB2");
    }

    [Fact]
    public async Task SearchAllAsync_follows_continue_tokens_and_stops_when_the_server_sends_none()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("archive/search", async (request, ct) =>
        {
            string? token = JsonNode.Parse(await request.Content!.ReadAsStringAsync(ct))!["continue_token"]?.GetValue<string>();
            string page = token is null ? """{"email_count":2,"emails":[{"email_id":"a"},{"email_id":"b"}],"continue_token":"next"}""" : """{"email_count":1,"emails":[{"email_id":"c"}]}""";
            return FakeHttpMessageHandler.Json(HttpStatusCode.OK, $$"""{"request_id":"r","data":{{page}}}""");
        });
        Smtp2GoClient client = TestClient.Create(handler);

        List<string?> ids = [];
        await foreach (ArchivedEmail email in client.Archive.SearchAllAsync(new ArchiveSearchRequest { Limit = 2 }, cancellationToken: TestContext.Current.CancellationToken))
        {
            ids.Add(email.EmailId);
        }

        ids.Should().Equal("a", "b", "c");
        handler.Requests.Should().HaveCount(2, because: "the second page carries no continue_token");
    }

    [Fact]
    public async Task DownloadOriginalAsync_gets_the_url_without_the_api_key_and_streams_the_body()
    {
        byte[] original = Encoding.ASCII.GetBytes("From: a@example.com\r\nTo: b@example.com\r\nSubject: test\r\n\r\nHello\r\n");
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("archive-attachment/abc123", (_, _) =>
        {
            HttpResponseMessage response = new(HttpStatusCode.OK) { Content = new ByteArrayContent(original) };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("message/rfc822");
            return Task.FromResult(response);
        });
        Smtp2GoClient client = TestClient.Create(handler);
        using MemoryStream destination = new();

        long written = await client.Archive.DownloadOriginalAsync(new ArchivedEmail { EmailId = "x", Url = DownloadUrl }, destination);

        written.Should().Be(original.Length);
        destination.ToArray().Should().Equal(original);
        RecordedRequest request = handler.LastRequest;
        request.Method.Should().Be(HttpMethod.Get);
        request.Uri.Should().Be(new Uri(DownloadUrl));
        request.Header("X-Smtp2go-Api-Key").Should().BeNull(because: "the download link is tried without credentials first");
        request.Header("Authorization").Should().BeNull();
        request.Body.Should().BeNull();
    }

    [Fact]
    public async Task DownloadOriginalAsync_retries_with_the_api_key_when_the_link_demands_it()
    {
        byte[] original = Encoding.ASCII.GetBytes("Subject: test\r\n\r\nHello\r\n");
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("archive-attachment/abc123", (request, _) =>
        {
            bool authenticated = request.Headers.Contains("X-Smtp2go-Api-Key");
            return Task.FromResult(authenticated ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(original) } : new HttpResponseMessage(HttpStatusCode.Unauthorized));
        });
        Smtp2GoClient client = TestClient.Create(handler);
        using MemoryStream destination = new();

        long written = await client.Archive.DownloadOriginalAsync(new ArchivedEmail { Url = DownloadUrl }, destination);

        written.Should().Be(original.Length);
        handler.Requests.Should().HaveCount(2);
        handler.Requests[0].Header("X-Smtp2go-Api-Key").Should().BeNull();
        handler.Requests[1].Header("X-Smtp2go-Api-Key").Should().Be(TestClient.ApiKey);
    }

    [Fact]
    public async Task DownloadOriginalAsync_reports_other_failures_as_api_exceptions()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("archive-attachment/abc123", HttpStatusCode.NotFound);
        Smtp2GoClient client = TestClient.Create(handler);
        using MemoryStream destination = new();

        Func<Task> act = () => client.Archive.DownloadOriginalAsync(new ArchivedEmail { Url = DownloadUrl }, destination);

        (await act.Should().ThrowAsync<Smtp2GoApiException>()).Which.StatusCode.Should().Be(404);
        handler.Requests.Should().HaveCount(1, because: "only 401 and 403 trigger the authenticated retry");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("...")]
    [InlineData("/relative/path")]
    public async Task DownloadOriginalAsync_rejects_emails_without_an_absolute_url(string? url)
    {
        Smtp2GoClient client = TestClient.Create(new FakeHttpMessageHandler());
        using MemoryStream destination = new();

        Func<Task> act = () => client.Archive.DownloadOriginalAsync(new ArchivedEmail { Url = url }, destination);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Null_and_blank_arguments_are_rejected()
    {
        Smtp2GoClient client = TestClient.Create(new FakeHttpMessageHandler());

        await ((Func<Task>)(() => client.Archive.SearchAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.Archive.GetAsync(" "))).Should().ThrowAsync<ArgumentException>();
        await ((Func<Task>)(() => client.Archive.DownloadOriginalAsync(null!, Stream.Null))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.Archive.DownloadOriginalAsync(new ArchivedEmail { Url = DownloadUrl }, null!))).Should().ThrowAsync<ArgumentNullException>();
        ((Action)(() => client.Archive.SearchAllAsync(null!))).Should().Throw<ArgumentNullException>();
    }
}
