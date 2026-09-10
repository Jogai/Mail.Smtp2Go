using System.Net;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Transport;

public class RawClientTests
{
    [Fact]
    public async Task Typed_overload_returns_the_envelope_with_request_id_and_data()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("stats/email_cycle", HttpStatusCode.OK, """{"request_id":"rid-1","data":{"cycle_max":1000}}""");
        Smtp2GoClient client = TestClient.Create(handler);

        ApiResponse<JsonElement> response = await client.Raw.SendAsync<JsonElement, JsonElement>("stats/email_cycle", default);

        response.RequestId.Should().Be("rid-1");
        response.Data.GetProperty("cycle_max").GetInt32().Should().Be(1000);
        response.Extra.Should().BeNull();
    }

    [Fact]
    public async Task Json_overload_returns_the_whole_document()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("stats/email_cycle", HttpStatusCode.OK, """{"request_id":"rid-2","data":[1,2]}""");
        Smtp2GoClient client = TestClient.Create(handler);

        using JsonDocument document = await client.Raw.SendJsonAsync("stats/email_cycle", TestClient.Json("""{"username":"u"}"""));

        document.RootElement.GetProperty("request_id").GetString().Should().Be("rid-2");
        document.RootElement.GetProperty("data").GetArrayLength().Should().Be(2);
        handler.LastRequest.Body.Should().Be("""{"username":"u"}""");
    }

    [Fact]
    public async Task Request_carries_json_headers_the_endpoint_descriptor_and_the_method()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler);

        using JsonDocument _ = await client.Raw.SendJsonAsync("email/send", default);

        RecordedRequest request = handler.LastRequest;
        request.Method.Should().Be(HttpMethod.Post);
        request.ContentType.Should().Be("application/json; charset=utf-8");
        request.Header("Accept").Should().Be("application/json");
        request.Endpoint.Should().Be(EndpointTable.Get("email/send"));
        request.AllowRetry.Should().BeFalse();
    }

    [Fact]
    public async Task Method_override_and_AllowRetry_are_applied()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler);

        using JsonDocument _ = await client.Raw.SendJsonAsync("api_keys/patch", default, Endpoint.Patch, new RequestOptions { AllowRetry = true });

        RecordedRequest request = handler.LastRequest;
        request.Method.Method.Should().Be("PATCH");
        request.Endpoint!.Method.Method.Should().Be("PATCH");
        request.Endpoint.Path.Should().Be("api_keys/patch");
        request.AllowRetry.Should().BeTrue();
    }

    [Fact]
    public async Task Diagnostics_see_start_and_completion_with_elapsed_time()
    {
        FakeHttpMessageHandler handler = new();
        RecordingDiagnostics diagnostics = new();
        Smtp2GoClient client = TestClient.Create(handler, diagnostics: diagnostics);

        using JsonDocument _ = await client.Raw.SendJsonAsync("stats/email_cycle", default);

        diagnostics.Started.Should().ContainSingle().Which.Endpoint.Path.Should().Be("stats/email_cycle");
        (Endpoint endpoint, int status, string? requestId, TimeSpan elapsed) = diagnostics.Completed.Single();
        endpoint.Path.Should().Be("stats/email_cycle");
        status.Should().Be(200);
        requestId.Should().Be("fake-request-id");
        elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public void Client_exposes_Raw_and_its_options()
    {
        Smtp2GoClient client = TestClient.Create(new FakeHttpMessageHandler());

        client.Raw.Should().NotBeNull();
        client.Options.ApiKey.Should().Be(TestClient.ApiKey);
        ((ISmtp2GoClient)client).Raw.Should().BeSameAs(client.Raw);
    }
}
