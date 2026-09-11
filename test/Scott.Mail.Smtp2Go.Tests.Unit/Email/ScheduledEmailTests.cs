using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Email;

public class ScheduledEmailTests
{
    [Fact]
    public void Search_request_serialises_every_documented_field()
    {
        ScheduledEmailSearchRequest request = new()
        {
            ScheduleId = "8fb29ea3-286d-493e-83c5-401f76859bb1",
            SearchSubject = "test 1",
            SearchRecipient = "recipient@example.com",
            SearchSender = "test@example.com",
            Limit = 100,
            Page = 2,
        };

        Golden.AssertMatches(JsonSerializer.Serialize(request, Smtp2GoJsonContext.Default.ScheduledEmailSearchRequest), "scheduled-search-request.json");
    }

    [Fact]
    public void Empty_search_request_serialises_as_an_empty_object()
    {
        JsonSerializer.Serialize(new ScheduledEmailSearchRequest(), Smtp2GoJsonContext.Default.ScheduledEmailSearchRequest).Should().Be("{}");
    }

    [Fact]
    public void Docs_search_response_deserialises()
    {
        ApiResponse<IReadOnlyList<ScheduledEmail>> response = JsonSerializer.Deserialize(Fixture.Read("Email/scheduled-search-response.json"), Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListScheduledEmail)!;

        ScheduledEmail item = response.Data.Should().ContainSingle().Which;
        item.ScheduleId.Should().Be("4d3b03a7-8663-4592-899a-b479ba6fcba9");
        item.Schedule.Should().Be(new DateTimeOffset(2025, 6, 30, 23, 11, 56, TimeSpan.Zero));
        item.Sender.Should().Be("test@example.com");
        item.Subject.Should().Be("test 1");
        item.Recipients.Should().Be("test@example2.com");
        item.ClientIp.Should().Be("127.0.0.1");
        item.Extra.Should().BeNull();
    }

    [Fact]
    public async Task SearchScheduledAsync_posts_to_the_scheduled_search_endpoint()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("email/scheduled/search", HttpStatusCode.OK, Fixture.Read("Email/scheduled-search-response.json"));
        Smtp2GoClient client = TestClient.Create(handler);

        ApiResponse<IReadOnlyList<ScheduledEmail>> response = await client.Email.SearchScheduledAsync(new ScheduledEmailSearchRequest { SearchSender = "test@example.com" });

        handler.LastRequest.Endpoint!.Path.Should().Be("email/scheduled/search");
        handler.LastRequest.Endpoint.Idempotent.Should().BeTrue();
        handler.LastRequest.Body.Should().Be("""{"search_sender":"test@example.com"}""");
        response.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchScheduledAllAsync_increments_page_until_an_empty_page()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("email/scheduled/search", async (request, ct) =>
        {
            int page = JsonNode.Parse(await request.Content!.ReadAsStringAsync(ct))!["page"]!.GetValue<int>();
            string items = page switch
            {
                1 => """{"schedule_id":"s1"},{"schedule_id":"s2"}""",
                2 => """{"schedule_id":"s3"},{"schedule_id":"s4"}""",
                3 => """{"schedule_id":"s5"}""",
                _ => string.Empty,
            };
            return FakeHttpMessageHandler.Json(HttpStatusCode.OK, $$"""{"request_id":"p{{page}}","data":[{{items}}]}""");
        });
        Smtp2GoClient client = TestClient.Create(handler);

        List<string?> ids = [];
        await foreach (ScheduledEmail item in client.Email.SearchScheduledAllAsync(new ScheduledEmailSearchRequest { SearchSender = "a" }, cancellationToken: TestContext.Current.CancellationToken))
        {
            ids.Add(item.ScheduleId);
        }

        ids.Should().Equal("s1", "s2", "s3", "s4", "s5");
        handler.Requests.Should().HaveCount(4, because: "pages 1 to 3 have items and page 4 is empty");
        handler.Requests.Select(r => JsonNode.Parse(r.Body!)!["page"]!.GetValue<int>()).Should().Equal(1, 2, 3, 4);
        handler.Requests.Should().OnlyContain(r => r.Body!.Contains("\"search_sender\":\"a\""));
    }

    [Fact]
    public async Task SearchScheduledAllAsync_stops_early_on_a_short_page_when_a_limit_is_set_and_starts_from_the_requested_page()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("email/scheduled/search", async (request, ct) =>
        {
            int page = JsonNode.Parse(await request.Content!.ReadAsStringAsync(ct))!["page"]!.GetValue<int>();
            string items = page == 3 ? """{"schedule_id":"a"},{"schedule_id":"b"}""" : """{"schedule_id":"c"}""";
            return FakeHttpMessageHandler.Json(HttpStatusCode.OK, $$"""{"request_id":"p","data":[{{items}}]}""");
        });
        Smtp2GoClient client = TestClient.Create(handler);

        List<ScheduledEmail> items = [];
        await foreach (ScheduledEmail item in client.Email.SearchScheduledAllAsync(new ScheduledEmailSearchRequest { Limit = 2, Page = 3 }, cancellationToken: TestContext.Current.CancellationToken))
        {
            items.Add(item);
        }

        items.Select(i => i.ScheduleId).Should().Equal("a", "b", "c");
        handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchScheduledAllAsync_is_lazy_and_honours_cancellation()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("email/scheduled/search", HttpStatusCode.OK, """{"request_id":"p","data":[{"schedule_id":"x"}]}""");
        Smtp2GoClient client = TestClient.Create(handler);
        using CancellationTokenSource cts = new();

        IAsyncEnumerable<ScheduledEmail> enumerable = client.Email.SearchScheduledAllAsync(new ScheduledEmailSearchRequest(), cancellationToken: cts.Token);
        handler.Requests.Should().BeEmpty(because: "nothing is sent until enumeration starts");

        await using IAsyncEnumerator<ScheduledEmail> enumerator = enumerable.GetAsyncEnumerator(TestContext.Current.CancellationToken);
        (await enumerator.MoveNextAsync()).Should().BeTrue();
        cts.Cancel();
        Func<Task> next = async () => await enumerator.MoveNextAsync();
        await next.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RemoveScheduledAsync_posts_the_id_and_returns_the_raw_data()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("email/scheduled/remove", HttpStatusCode.OK, Fixture.Read("Email/scheduled-remove-response.json"));
        Smtp2GoClient client = TestClient.Create(handler);

        ApiResponse<JsonElement> response = await client.Email.RemoveScheduledAsync("fe7d54d0-8f06-40c5-a675-d72f183e8ebf");

        handler.LastRequest.Endpoint!.Path.Should().Be("email/scheduled/remove");
        Golden.AssertMatches(handler.LastRequest.Body!, "scheduled-remove-request.json");
        response.RequestId.Should().Be("d1e2f3a4-b5c6-4d7e-8f90-a1b2c3d4e5f6");
        response.Data.GetProperty("message").GetString().Should().Be("Scheduled email removed");
    }

    [Fact]
    public async Task Null_or_blank_arguments_are_rejected()
    {
        Smtp2GoClient client = TestClient.Create(new FakeHttpMessageHandler());

        Func<Task> nullSearch = () => client.Email.SearchScheduledAsync(null!);
        Func<Task> blankId = () => client.Email.RemoveScheduledAsync(" ");
        Func<Task> nullAll = async () =>
        {
            await foreach (ScheduledEmail _ in client.Email.SearchScheduledAllAsync(null!, cancellationToken: TestContext.Current.CancellationToken))
            {
            }
        };

        await nullSearch.Should().ThrowAsync<ArgumentNullException>();
        await blankId.Should().ThrowAsync<ArgumentException>();
        await nullAll.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Schedule_timestamps_in_the_docs_space_separated_form_are_read_too()
    {
        ApiResponse<IReadOnlyList<ScheduledEmail>> response = JsonSerializer.Deserialize(
            """{"request_id":"r","data":[{"schedule_id":"s","schedule":"2025-09-10 13:15:00 +1200"}]}""", Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListScheduledEmail)!;

        response.Data[0].Schedule.Should().Be(DateTimeOffset.Parse("2025-09-10T01:15:00Z", CultureInfo.InvariantCulture));
    }
}
