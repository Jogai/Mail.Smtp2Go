using System.Net;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Transport;

public class ErrorMappingTests
{
    private const string Path = "stats/email_cycle";

    private static async Task<TException> CallAndCatch<TException>(FakeHttpMessageHandler handler, RecordingDiagnostics? diagnostics = null)
        where TException : Exception
    {
        Smtp2GoClient client = TestClient.Create(handler, diagnostics: diagnostics);
        Func<Task> act = async () => await client.Raw.SendAsync<JsonElement, JsonElement>(Path, default);
        return (await act.Should().ThrowAsync<TException>()).Which;
    }

    [Fact]
    public async Task Success_with_a_parseable_body_returns_the_envelope()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond(Path, HttpStatusCode.OK, Fixture.Read("Transport/success-with-extra.json"));
        RecordingDiagnostics diagnostics = new();
        Smtp2GoClient client = TestClient.Create(handler, diagnostics: diagnostics);

        ApiResponse<JsonElement> response = await client.Raw.SendAsync<JsonElement, JsonElement>(Path, default);

        response.RequestId.Should().Be("6c7d8e9f-0a1b-2c3d-4e5f-6a7b8c9d0e1f");
        response.Data.GetProperty("email_id").GetString().Should().Be("1er8bV-6Tw0Mi-7h");
        diagnostics.Completed.Should().ContainSingle().Which.RequestId.Should().Be(response.RequestId);
        diagnostics.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task Nested_error_body_with_field_validation_errors_maps_to_Smtp2GoApiException()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond(Path, HttpStatusCode.BadRequest, Fixture.Read("Transport/error-nested-validation.json"));
        RecordingDiagnostics diagnostics = new();

        Smtp2GoApiException exception = await CallAndCatch<Smtp2GoApiException>(handler, diagnostics);

        exception.Should().BeOfType<Smtp2GoApiException>();
        exception.StatusCode.Should().Be(400);
        exception.Path.Should().Be(Path);
        exception.RequestId.Should().Be("0d1a2b3c-4d5e-6f70-8192-a3b4c5d6e7f8");
        exception.ErrorCode.Should().Be("E_ApiResponseCodes.NON_VALIDATING_IN_PAYLOAD");
        exception.Code.Should().Be(Smtp2GoErrorCode.NonValidatingInPayload);
        exception.Message.Should().Contain("Failed to validate JSON payload.").And.Contain("400").And.Contain(Path);
        exception.FieldValidationErrors.Should().ContainSingle().Which.Should().Be(new FieldValidationError("to", "'to' is a required property"));
        exception.RawBody.Should().Contain("field_validation_errors");
        diagnostics.Failed.Should().ContainSingle().Which.RequestId.Should().Be(exception.RequestId);
    }

    [Fact]
    public async Task Flat_error_body_maps_to_the_same_exception()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond(Path, HttpStatusCode.BadRequest, Fixture.Read("Transport/error-flat.json"));

        Smtp2GoApiException exception = await CallAndCatch<Smtp2GoApiException>(handler);

        exception.Should().BeOfType<Smtp2GoApiException>();
        exception.RequestId.Should().Be("5f0a9c2e-1b3d-4e6f-8a7b-9c0d1e2f3a4b");
        exception.Error!.Message.Should().Be("Invalid API key.");
        exception.ErrorCode.Should().Be("E_ApiResponseCodes.API_KEY_INVALID");
        exception.Code.Should().Be(Smtp2GoErrorCode.Unknown);
        exception.FieldValidationErrors.Should().BeEmpty();
    }

    [Fact]
    public async Task Status_401_maps_to_Smtp2GoAuthenticationException()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond(Path, HttpStatusCode.Unauthorized, Fixture.Read("Transport/error-flat.json"));

        Smtp2GoAuthenticationException exception = await CallAndCatch<Smtp2GoAuthenticationException>(handler);

        exception.StatusCode.Should().Be(401);
        exception.RequestId.Should().NotBeNull();
    }

    [Fact]
    public async Task Status_403_with_ENDPOINT_PERMISSION_DENIED_maps_to_Smtp2GoPermissionException_naming_the_endpoint()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond(Path, HttpStatusCode.Forbidden, Fixture.Read("Transport/error-nested-permission.json"));

        Smtp2GoPermissionException exception = await CallAndCatch<Smtp2GoPermissionException>(handler);

        exception.StatusCode.Should().Be(403);
        exception.Path.Should().Be(Path);
        exception.Code.Should().Be(Smtp2GoErrorCode.EndpointPermissionDenied);
        exception.Message.Should().Contain($"permission to call '{Path}'");
        exception.RequestId.Should().Be("aa253464-0bd0-467a-b24b-6159dcd7be60");
    }

    [Fact]
    public async Task ENDPOINT_PERMISSION_DENIED_with_another_status_still_maps_to_Smtp2GoPermissionException()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond(Path, HttpStatusCode.BadRequest, Fixture.Read("Transport/error-nested-permission.json"));

        Smtp2GoPermissionException exception = await CallAndCatch<Smtp2GoPermissionException>(handler);

        exception.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Status_429_with_Retry_After_seconds_maps_to_Smtp2GoRateLimitException()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond(
            Path,
            (HttpStatusCode)429,
            """{"request_id":"rl-1","data":{"error":"Rate limit exceeded","error_code":"E_ApiResponseCodes.RATE_LIMITED"}}""",
            response => response.Headers.Add("Retry-After", "7"));

        Smtp2GoRateLimitException exception = await CallAndCatch<Smtp2GoRateLimitException>(handler);

        exception.StatusCode.Should().Be(429);
        exception.RetryAfter.Should().Be(TimeSpan.FromSeconds(7));
        exception.RequestId.Should().Be("rl-1");
    }

    [Fact]
    public async Task Status_429_with_an_http_date_Retry_After_yields_a_non_negative_delay()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond(
            Path,
            (HttpStatusCode)429,
            "{}",
            response => response.Headers.Add("Retry-After", DateTimeOffset.UtcNow.AddSeconds(30).ToString("R")));

        Smtp2GoRateLimitException exception = await CallAndCatch<Smtp2GoRateLimitException>(handler);

        exception.RetryAfter.Should().NotBeNull();
        exception.RetryAfter!.Value.Should().BeGreaterThan(TimeSpan.Zero).And.BeLessThanOrEqualTo(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task Status_429_without_Retry_After_has_a_null_delay()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond(Path, (HttpStatusCode)429, "{}");

        Smtp2GoRateLimitException exception = await CallAndCatch<Smtp2GoRateLimitException>(handler);

        exception.RetryAfter.Should().BeNull();
    }

    [Fact]
    public async Task Unparseable_error_body_keeps_the_raw_body_and_no_code()
    {
        string garbage = Fixture.Read("Transport/error-garbage.txt");
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond(Path, HttpStatusCode.BadGateway, garbage);

        Smtp2GoApiException exception = await CallAndCatch<Smtp2GoApiException>(handler);

        exception.Should().BeOfType<Smtp2GoApiException>();
        exception.StatusCode.Should().Be(502);
        exception.ErrorCode.Should().BeNull();
        exception.Code.Should().Be(Smtp2GoErrorCode.Unknown);
        exception.RequestId.Should().BeNull();
        exception.RawBody.Should().Be(garbage);
        exception.Error!.IsParsed.Should().BeFalse();
        exception.Message.Should().Contain("502 Bad Gateway");
    }

    [Fact]
    public async Task Error_without_a_body_still_maps_by_status()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond(Path, HttpStatusCode.ServiceUnavailable, body: null);

        Smtp2GoApiException exception = await CallAndCatch<Smtp2GoApiException>(handler);

        exception.StatusCode.Should().Be(503);
        exception.Message.Should().Contain("no response body");
    }

    [Fact]
    public async Task Success_with_an_empty_body_throws_Smtp2GoApiException()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond(Path, HttpStatusCode.OK, "null");

        Smtp2GoApiException exception = await CallAndCatch<Smtp2GoApiException>(handler);

        exception.StatusCode.Should().Be(200);
        exception.Message.Should().Contain("empty body");
    }

    [Fact]
    public async Task Success_with_a_malformed_body_throws_Smtp2GoApiException_wrapping_the_JsonException()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond(Path, HttpStatusCode.OK, "{not json");

        Smtp2GoApiException exception = await CallAndCatch<Smtp2GoApiException>(handler);

        exception.InnerException.Should().BeAssignableTo<JsonException>();
        exception.Message.Should().Contain("could not be parsed");
    }

    [Fact]
    public async Task Success_missing_request_id_is_a_parse_failure()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond(Path, HttpStatusCode.OK, """{"data":{}}""");

        Smtp2GoApiException exception = await CallAndCatch<Smtp2GoApiException>(handler);

        exception.InnerException.Should().BeAssignableTo<JsonException>();
    }

    [Fact]
    public async Task Transport_failures_are_not_wrapped()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond(Path, (_, _) => throw new HttpRequestException("connection refused"));
        RecordingDiagnostics diagnostics = new();

        HttpRequestException exception = await CallAndCatch<HttpRequestException>(handler, diagnostics);

        exception.Message.Should().Be("connection refused");
        diagnostics.Failed.Should().ContainSingle().Which.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void All_exceptions_derive_from_Smtp2GoException()
    {
        typeof(Smtp2GoApiException).Should().BeDerivedFrom<Smtp2GoException>();
        typeof(Smtp2GoAuthenticationException).Should().BeDerivedFrom<Smtp2GoApiException>();
        typeof(Smtp2GoPermissionException).Should().BeDerivedFrom<Smtp2GoApiException>();
        typeof(Smtp2GoRateLimitException).Should().BeDerivedFrom<Smtp2GoApiException>();
        typeof(Smtp2GoValidationException).Should().BeDerivedFrom<Smtp2GoException>();
    }
}
