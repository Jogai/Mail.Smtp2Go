using System.Text.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Transport;

public class BodyValidationTests
{
    private static JsonElement BodyOfSize(int bytes)
    {
        return TestClient.Json("{\"blob\":\"" + new string('a', bytes) + "\"}");
    }

    [Fact]
    public async Task Body_over_one_megabyte_on_a_non_email_endpoint_throws_locally()
    {
        FakeHttpMessageHandler handler = new();
        RecordingDiagnostics diagnostics = new();
        Smtp2GoClient client = TestClient.Create(handler, diagnostics: diagnostics);

        Func<Task> act = async () => await client.Raw.SendJsonAsync("stats/email_cycle", BodyOfSize(1024 * 1024));

        Smtp2GoValidationException exception = (await act.Should().ThrowAsync<Smtp2GoValidationException>()).Which;
        exception.Errors.Should().ContainSingle().Which.Should().Contain("1048576 bytes");
        handler.Requests.Should().BeEmpty();
        diagnostics.ValidationFailures.Should().ContainSingle().Which.Endpoint.Path.Should().Be("stats/email_cycle");
        diagnostics.Started.Should().BeEmpty();
    }

    [Fact]
    public async Task Same_body_on_an_email_endpoint_is_within_the_fifty_megabyte_limit()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler);

        using JsonDocument _ = await client.Raw.SendJsonAsync("email/send", BodyOfSize(1024 * 1024));

        handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task Body_limit_is_not_checked_when_client_side_validation_is_off()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler, o => o.ClientSideValidation = false);

        using JsonDocument _ = await client.Raw.SendJsonAsync("stats/email_cycle", BodyOfSize(1024 * 1024));

        handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task IRequestValidator_on_the_model_is_consulted_before_sending()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler, o => o.AdditionalJsonTypeInfoResolver = ProbeJsonContext.Default);

        Func<Task> act = async () => await client.Raw.SendAsync<ProbeRequest, ProbeData>("probe/echo", new ProbeRequest(null));

        (await act.Should().ThrowAsync<Smtp2GoValidationException>()).Which.Errors.Should().ContainSingle().Which.Should().Be("name is required for probe/echo.");
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task IRequestValidator_is_skipped_when_client_side_validation_is_off()
    {
        FakeHttpMessageHandler handler = new();
        handler.Respond("probe/echo", System.Net.HttpStatusCode.OK, """{"request_id":"r","data":{"echo":null}}""");
        Smtp2GoClient client = TestClient.Create(handler, o =>
        {
            o.ClientSideValidation = false;
            o.AdditionalJsonTypeInfoResolver = ProbeJsonContext.Default;
        });

        ApiResponse<ProbeData> response = await client.Raw.SendAsync<ProbeRequest, ProbeData>("probe/echo", new ProbeRequest(null));

        response.RequestId.Should().Be("r");
        handler.LastRequest.Body.Should().Be("{}");
    }

    [Fact]
    public async Task Additional_resolver_types_serialise_with_snake_case_and_deserialise_the_envelope()
    {
        FakeHttpMessageHandler handler = new();
        handler.Respond("probe/echo", System.Net.HttpStatusCode.OK, """{"request_id":"r-1","data":{"echo":"hi"},"extra":true}""");
        Smtp2GoClient client = TestClient.Create(handler, o => o.AdditionalJsonTypeInfoResolver = ProbeJsonContext.Default);

        ApiResponse<ProbeData> response = await client.Raw.SendAsync<ProbeRequest, ProbeData>("probe/echo", new ProbeRequest("hi"));

        handler.LastRequest.Body.Should().Be("""{"name":"hi"}""");
        response.Data.Echo.Should().Be("hi");
        response.Extra.Should().ContainKey("extra");
    }

    [Fact]
    public async Task Unregistered_types_are_refused_with_a_helpful_message()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler);

        Func<Task> act = async () => await client.Raw.SendAsync<ProbeRequest, ProbeData>("probe/echo", new ProbeRequest("x"));

        (await act.Should().ThrowAsync<NotSupportedException>()).Which.Message.Should().Contain("AdditionalJsonTypeInfoResolver");
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public void Endpoint_body_limits_follow_the_family()
    {
        EndpointTable.Get("email/send").MaxBodyBytes.Should().Be(50L * 1024 * 1024);
        EndpointTable.Get("email/not/registered").MaxBodyBytes.Should().Be(50L * 1024 * 1024);
        EndpointTable.Get("stats/email_cycle").MaxBodyBytes.Should().Be(1024 * 1024);
    }
}
