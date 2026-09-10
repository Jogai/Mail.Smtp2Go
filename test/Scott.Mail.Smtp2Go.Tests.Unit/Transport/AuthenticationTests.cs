using System.Text.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Transport;

public class AuthenticationTests
{
    [Fact]
    public async Task Default_scheme_sends_the_key_in_the_X_Smtp2go_Api_Key_header_only()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler);

        using JsonDocument _ = await client.Raw.SendJsonAsync("stats/email_cycle", default);

        RecordedRequest request = handler.LastRequest;
        request.Header("X-Smtp2go-Api-Key").Should().Be(TestClient.ApiKey);
        request.Header("Authorization").Should().BeNull();
        request.Body.Should().Be("{}");
        request.Body.Should().NotContain(TestClient.ApiKey);
        request.Uri.Query.Should().BeEmpty();
    }

    [Fact]
    public async Task Bearer_scheme_sends_an_Authorization_header_only()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler, o => o.AuthenticationScheme = AuthenticationScheme.Bearer);

        using JsonDocument _ = await client.Raw.SendJsonAsync("stats/email_cycle", default);

        RecordedRequest request = handler.LastRequest;
        request.Header("Authorization").Should().Be("Bearer " + TestClient.ApiKey);
        request.Header("X-Smtp2go-Api-Key").Should().BeNull();
        request.Body.Should().NotContain(TestClient.ApiKey);
    }

    [Fact]
    public async Task ApiKeyOverride_replaces_the_key_for_one_call()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler);

        using JsonDocument first = await client.Raw.SendJsonAsync("stats/email_cycle", default, options: new RequestOptions { ApiKeyOverride = "api-override" });
        using JsonDocument second = await client.Raw.SendJsonAsync("stats/email_cycle", default);

        handler.Requests[0].Header("X-Smtp2go-Api-Key").Should().Be("api-override");
        handler.Requests[1].Header("X-Smtp2go-Api-Key").Should().Be(TestClient.ApiKey);
    }

    [Fact]
    public async Task Empty_ApiKeyOverride_is_rejected_before_sending()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler);

        Func<Task> act = async () => await client.Raw.SendJsonAsync("stats/email_cycle", default, options: new RequestOptions { ApiKeyOverride = " " });

        await act.Should().ThrowAsync<ArgumentException>();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Body_never_contains_api_key_or_version_fields()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler);

        using JsonDocument _ = await client.Raw.SendJsonAsync("stats/email_cycle", TestClient.Json("""{"username":"alice"}"""));

        using JsonDocument body = JsonDocument.Parse(handler.LastRequest.Body!);
        body.RootElement.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(["username"]);
    }
}
