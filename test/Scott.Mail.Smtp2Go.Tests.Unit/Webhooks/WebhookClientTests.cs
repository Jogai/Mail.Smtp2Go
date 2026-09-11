using System.Net;
using System.Text.Json.Nodes;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Tests.Unit.Email;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Webhooks;

public class WebhookClientTests
{
    [Fact]
    public async Task ViewAsync_posts_an_empty_object_and_reads_the_array_shape()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("webhook/view", HttpStatusCode.OK, Fixture.Read("Webhooks/view-response-array.json"));
        Smtp2GoClient client = TestClient.Create(handler);

        ApiResponse<IReadOnlyList<Webhook>> response = await client.Webhooks.ViewAsync();

        handler.LastRequest.Endpoint!.Path.Should().Be("webhook/view");
        handler.LastRequest.Body.Should().Be("{}");
        response.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task ViewAsync_merges_the_subaccount_id_into_the_empty_body()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("webhook/view", HttpStatusCode.OK, Fixture.Read("Webhooks/view-response-object.json"));
        Smtp2GoClient client = TestClient.Create(handler);

        await client.Webhooks.ViewAsync(new RequestOptions { SubaccountId = "sub-1" });

        handler.LastRequest.Body.Should().Be("""{"subaccount_id":"sub-1"}""");
    }

    [Fact]
    public async Task AddAsync_posts_the_request_and_returns_the_webhook()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("webhook/add", HttpStatusCode.OK, Fixture.Read("Webhooks/add-response.json"));
        Smtp2GoClient client = TestClient.Create(handler);

        ApiResponse<Webhook> response = await client.Webhooks.AddAsync(WebhookModelTests.FullAddRequest);

        handler.LastRequest.Endpoint!.Path.Should().Be("webhook/add");
        handler.LastRequest.Endpoint.Idempotent.Should().BeFalse();
        Golden.AssertMatchesFixture(handler.LastRequest.Body!, "Webhooks/add-request-full.json");
        response.Data.Id.Should().Be(4320);
    }

    [Fact]
    public async Task EditAsync_posts_to_the_edit_endpoint()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("webhook/edit", HttpStatusCode.OK, Fixture.Read("Webhooks/edit-response.json"));
        Smtp2GoClient client = TestClient.Create(handler);

        ApiResponse<Webhook> response = await client.Webhooks.EditAsync(new WebhookEditRequest { Id = 4320, Url = "https://example.com/test-webhook-2" });

        handler.LastRequest.Endpoint!.Path.Should().Be("webhook/edit");
        handler.LastRequest.Body.Should().Be("""{"id":4320,"url":"https://example.com/test-webhook-2"}""");
        response.Data.Url.Should().Be("https://example.com/test-webhook-2");
    }

    [Fact]
    public async Task RemoveAsync_posts_the_id_and_returns_the_removed_webhook()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("webhook/remove", HttpStatusCode.OK, Fixture.Read("Webhooks/remove-response.json"));
        Smtp2GoClient client = TestClient.Create(handler);

        ApiResponse<Webhook> response = await client.Webhooks.RemoveAsync(4317);

        handler.LastRequest.Endpoint!.Path.Should().Be("webhook/remove");
        Golden.AssertMatchesFixture(handler.LastRequest.Body!, "Webhooks/remove-request.json");
        response.Data.Id.Should().Be(4317);
        response.Data.Events.Should().Equal(WebhookEmailEvent.Spam);
    }

    [Fact]
    public async Task AddOrUpdateByUrlAsync_edits_the_webhook_with_the_same_url()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler()
            .Respond("webhook/view", HttpStatusCode.OK, Fixture.Read("Webhooks/view-response-array.json"))
            .Respond("webhook/edit", HttpStatusCode.OK, Fixture.Read("Webhooks/edit-response.json"));
        Smtp2GoClient client = TestClient.Create(handler);

        await client.Webhooks.AddOrUpdateByUrlAsync(WebhookModelTests.FullAddRequest);

        handler.Requests.Select(r => r.Endpoint!.Path).Should().Equal("webhook/view", "webhook/edit");
        JsonNode body = JsonNode.Parse(handler.LastRequest.Body!)!;
        body["id"]!.GetValue<long>().Should().Be(4320, because: "the second webhook in the fixture has the request's url");
        body["url"]!.GetValue<string>().Should().Be(WebhookModelTests.FullAddRequest.Url);
        body["output_format"]!.GetValue<string>().Should().Be("json");
    }

    [Fact]
    public async Task AddOrUpdateByUrlAsync_adds_when_no_webhook_has_the_url()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler()
            .Respond("webhook/view", HttpStatusCode.OK, Fixture.Read("Webhooks/view-response-object.json"))
            .Respond("webhook/add", HttpStatusCode.OK, Fixture.Read("Webhooks/add-response.json"));
        Smtp2GoClient client = TestClient.Create(handler);

        ApiResponse<Webhook> response = await client.Webhooks.AddOrUpdateByUrlAsync(WebhookModelTests.FullAddRequest);

        handler.Requests.Select(r => r.Endpoint!.Path).Should().Equal("webhook/view", "webhook/add");
        response.Data.Id.Should().Be(4320);
    }

    [Fact]
    public async Task Validation_failures_are_raised_before_any_request()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler);

        Func<Task> act = () => client.Webhooks.AddAsync(new WebhookAddRequest { Url = "nope" });

        (await act.Should().ThrowAsync<Smtp2GoValidationException>()).Which.Errors.Should().Contain("url must be an absolute http or https URL.");
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Null_requests_are_rejected()
    {
        Smtp2GoClient client = TestClient.Create(new FakeHttpMessageHandler());

        await ((Func<Task>)(() => client.Webhooks.AddAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.Webhooks.EditAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
        await ((Func<Task>)(() => client.Webhooks.AddOrUpdateByUrlAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
    }
}
