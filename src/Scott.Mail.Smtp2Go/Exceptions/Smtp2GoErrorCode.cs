using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// Known values of the API's <c>error_code</c>, without the <c>E_ApiResponseCodes.</c> prefix. Unrecognised codes map to <see cref="Unknown"/>;
/// the raw string is always available on <see cref="ApiError.ErrorCode"/>.
/// </summary>
[JsonConverter(typeof(TolerantEnumConverter<Smtp2GoErrorCode>))]
public enum Smtp2GoErrorCode
{
    /// <summary>No code, or one this library does not know.</summary>
    Unknown = 0,

    /// <summary>The API key lacks permission for the endpoint.</summary>
    [JsonStringEnumMemberName("ENDPOINT_PERMISSION_DENIED")]
    EndpointPermissionDenied = 1,

    /// <summary>The request body failed validation; see <see cref="ApiError.FieldValidationErrors"/>.</summary>
    [JsonStringEnumMemberName("NON_VALIDATING_IN_PAYLOAD")]
    NonValidatingInPayload = 2,
}
