using System.Text.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Transport;

public class SubaccountInjectionTests
{
    private static readonly Endpoint Accepting = new("webhook/add", HttpMethod.Post, Idempotent: false, AcceptsSubaccountId: true, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes);
    private static readonly Endpoint NotAccepting = new("stats/email_cycle", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes);

    [Fact]
    public async Task Request_option_is_injected_for_an_accepting_endpoint()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoConnection connection = TestClient.CreateConnection(handler);

        await connection.SendAsync<JsonElement, JsonElement>(Accepting, TestClient.Json("""{"url":"https://example.test/hook"}"""), new RequestOptions { SubaccountId = "sub-1" }, CancellationToken.None);

        using JsonDocument body = JsonDocument.Parse(handler.LastRequest.Body!);
        body.RootElement.GetProperty("subaccount_id").GetString().Should().Be("sub-1");
        body.RootElement.GetProperty("url").GetString().Should().Be("https://example.test/hook");
    }

    [Fact]
    public async Task Default_subaccount_is_injected_when_the_request_sets_none()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoConnection connection = TestClient.CreateConnection(handler, o => o.DefaultSubaccountId = "default-sub");

        await connection.SendAsync<JsonElement, JsonElement>(Accepting, TestClient.Json("{}"), null, CancellationToken.None);

        using JsonDocument body = JsonDocument.Parse(handler.LastRequest.Body!);
        body.RootElement.GetProperty("subaccount_id").GetString().Should().Be("default-sub");
    }

    [Fact]
    public async Task Request_option_wins_over_the_default_subaccount()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoConnection connection = TestClient.CreateConnection(handler, o => o.DefaultSubaccountId = "default-sub");

        await connection.SendAsync<JsonElement, JsonElement>(Accepting, TestClient.Json("{}"), new RequestOptions { SubaccountId = "sub-2" }, CancellationToken.None);

        using JsonDocument body = JsonDocument.Parse(handler.LastRequest.Body!);
        body.RootElement.GetProperty("subaccount_id").GetString().Should().Be("sub-2");
    }

    [Fact]
    public async Task Null_body_with_a_subaccount_becomes_an_object_with_only_subaccount_id()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoConnection connection = TestClient.CreateConnection(handler);

        await connection.SendAsync<JsonElement, JsonElement>(Accepting, default, new RequestOptions { SubaccountId = "sub-3" }, CancellationToken.None);

        handler.LastRequest.Body.Should().Be("""{"subaccount_id":"sub-3"}""");
    }

    [Fact]
    public async Task Not_injected_for_an_endpoint_that_does_not_accept_it_and_diagnostics_are_told()
    {
        FakeHttpMessageHandler handler = new();
        RecordingDiagnostics diagnostics = new();
        Smtp2GoConnection connection = TestClient.CreateConnection(handler, diagnostics: diagnostics);

        await connection.SendAsync<JsonElement, JsonElement>(NotAccepting, TestClient.Json("{}"), new RequestOptions { SubaccountId = "sub-4" }, CancellationToken.None);

        handler.LastRequest.Body.Should().Be("{}");
        diagnostics.SubaccountIdIgnoredFor.Should().ContainSingle().Which.Path.Should().Be("stats/email_cycle");
    }

    [Fact]
    public async Task Not_injected_when_no_subaccount_is_configured()
    {
        FakeHttpMessageHandler handler = new();
        RecordingDiagnostics diagnostics = new();
        Smtp2GoConnection connection = TestClient.CreateConnection(handler, diagnostics: diagnostics);

        await connection.SendAsync<JsonElement, JsonElement>(Accepting, TestClient.Json("""{"url":"x"}"""), null, CancellationToken.None);

        handler.LastRequest.Body.Should().Be("""{"url":"x"}""");
        diagnostics.SubaccountIdIgnoredFor.Should().BeEmpty();
    }

    [Fact]
    public async Task Array_body_cannot_take_a_subaccount_and_fails_before_sending()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoConnection connection = TestClient.CreateConnection(handler);

        Func<Task> act = async () => await connection.SendAsync<JsonElement, JsonElement>(Accepting, TestClient.Json("[1,2]"), new RequestOptions { SubaccountId = "sub-5" }, CancellationToken.None);

        (await act.Should().ThrowAsync<Smtp2GoValidationException>()).Which.Errors.Should().ContainSingle().Which.Should().Contain("subaccount_id");
        handler.Requests.Should().BeEmpty();
    }
}
