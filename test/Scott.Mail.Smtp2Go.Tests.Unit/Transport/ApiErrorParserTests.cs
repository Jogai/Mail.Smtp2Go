using Scott.Mail.Smtp2Go.Tests.Shared;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Transport;

public class ApiErrorParserTests
{
    [Fact]
    public void Nested_shape_is_read_from_data()
    {
        ApiError error = ApiErrorParser.Parse(Fixture.Read("Transport/error-nested-permission.json"));

        error.RequestId.Should().Be("aa253464-0bd0-467a-b24b-6159dcd7be60");
        error.Message.Should().Be("You do not have permission to access this endpoint.");
        error.ErrorCode.Should().Be("E_ApiResponseCodes.ENDPOINT_PERMISSION_DENIED");
        error.Code.Should().Be(Smtp2GoErrorCode.EndpointPermissionDenied);
        error.IsParsed.Should().BeTrue();
    }

    [Fact]
    public void Flat_shape_is_read_from_the_root()
    {
        ApiError error = ApiErrorParser.Parse(Fixture.Read("Transport/error-flat.json"));

        error.RequestId.Should().Be("5f0a9c2e-1b3d-4e6f-8a7b-9c0d1e2f3a4b");
        error.Message.Should().Be("Invalid API key.");
        error.Code.Should().Be(Smtp2GoErrorCode.Unknown);
    }

    [Fact]
    public void Field_validation_errors_as_an_array_are_all_read()
    {
        ApiError error = ApiErrorParser.Parse(Fixture.Read("Transport/error-nested-validation-array.json"));

        error.FieldValidationErrors.Should().Equal(
            new FieldValidationError("sender", "'sender' is a required property"),
            new FieldValidationError("to", "'to' must contain at least one address"));
    }

    [Fact]
    public void Field_validation_errors_as_a_map_are_flattened()
    {
        ApiError error = ApiErrorParser.Parse(Fixture.Read("Transport/error-nested-validation-map.json"));

        error.FieldValidationErrors.Should().Equal(
            new FieldValidationError("subject", "must be a string"),
            new FieldValidationError("attachments", "filename is required"),
            new FieldValidationError("attachments", "fileblob must be base64"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<html>oops</html>")]
    [InlineData("[1,2,3]")]
    [InlineData("\"just a string\"")]
    public void Unparseable_or_non_object_bodies_keep_only_the_raw_body(string body)
    {
        ApiError error = ApiErrorParser.Parse(body);

        error.IsParsed.Should().BeFalse();
        error.RawBody.Should().Be(body);
        error.RequestId.Should().BeNull();
    }

    [Fact]
    public void Null_body_is_tolerated()
    {
        ApiErrorParser.Parse(null).IsParsed.Should().BeFalse();
    }

    [Fact]
    public void Non_string_error_values_are_kept_as_raw_json()
    {
        ApiError error = ApiErrorParser.Parse("""{"error":{"reason":"nested"},"error_code":42}""");

        error.Message.Should().Be("""{"reason":"nested"}""");
        error.ErrorCode.Should().Be("42");
    }

    [Theory]
    [InlineData("E_ApiResponseCodes.ENDPOINT_PERMISSION_DENIED", Smtp2GoErrorCode.EndpointPermissionDenied)]
    [InlineData("e_apiresponsecodes.endpoint_permission_denied", Smtp2GoErrorCode.EndpointPermissionDenied)]
    [InlineData("ENDPOINT_PERMISSION_DENIED", Smtp2GoErrorCode.EndpointPermissionDenied)]
    [InlineData("EndpointPermissionDenied", Smtp2GoErrorCode.EndpointPermissionDenied)]
    [InlineData("E_ApiResponseCodes.NON_VALIDATING_IN_PAYLOAD", Smtp2GoErrorCode.NonValidatingInPayload)]
    [InlineData("E_ApiResponseCodes.SOMETHING_NEW", Smtp2GoErrorCode.Unknown)]
    [InlineData("", Smtp2GoErrorCode.Unknown)]
    [InlineData(null, Smtp2GoErrorCode.Unknown)]
    public void Error_codes_parse_tolerantly(string? code, Smtp2GoErrorCode expected)
    {
        ApiErrorParser.ParseCode(code).Should().Be(expected);
    }
}
