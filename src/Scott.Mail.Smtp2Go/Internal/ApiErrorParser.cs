using System.Text.Json;
using Scott.Mail.Smtp2Go.Json;

namespace Scott.Mail.Smtp2Go;

/// <summary>Turns a non-success response body into an <see cref="ApiError"/>, tolerating the nested, flat and unparseable shapes.</summary>
internal static class ApiErrorParser
{
    private const string ErrorCodePrefix = "E_ApiResponseCodes.";

    public static ApiError Parse(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return new ApiError { RawBody = body };
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(body!, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return new ApiError { RawBody = body };
            }

            string? requestId = GetString(root, "request_id");

            // Nested shape first: {request_id, data: {error, error_code, field_validation_errors}}; then the flat shape on the root.
            ApiError? nested = root.TryGetProperty("data", out JsonElement data) && data.ValueKind == JsonValueKind.Object ? ReadFields(data) : null;
            ApiError fields = nested is { IsParsed: true } ? nested : ReadFields(root);

            return fields with { RequestId = requestId, RawBody = body };
        }
        catch (JsonException)
        {
            return new ApiError { RawBody = body };
        }
    }

    public static Smtp2GoErrorCode ParseCode(string? errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            return Smtp2GoErrorCode.Unknown;
        }

        string text = errorCode!.Trim();
        if (text.StartsWith(ErrorCodePrefix, StringComparison.OrdinalIgnoreCase))
        {
            text = text.Substring(ErrorCodePrefix.Length);
        }

        return EnumNameTable<Smtp2GoErrorCode>.TryParse(text, out Smtp2GoErrorCode code) ? code : Smtp2GoErrorCode.Unknown;
    }

    private static ApiError ReadFields(JsonElement source)
    {
        string? errorCode = GetString(source, "error_code");
        return new ApiError
        {
            Message = GetString(source, "error"),
            ErrorCode = errorCode,
            Code = ParseCode(errorCode),
            FieldValidationErrors = source.TryGetProperty("field_validation_errors", out JsonElement fieldErrors) ? ReadFieldErrors(fieldErrors) : [],
        };
    }

    /// <summary>
    /// Accepts the documented object (<c>{fieldname, message}</c>), an array of such objects, a map of field name to message (or to a list of
    /// messages), or a bare string.
    /// </summary>
    private static List<FieldValidationError> ReadFieldErrors(JsonElement element)
    {
        List<FieldValidationError> result = [];
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                result.Add(new FieldValidationError(null, element.GetString() ?? string.Empty));
                break;
            case JsonValueKind.Array:
                foreach (JsonElement item in element.EnumerateArray())
                {
                    result.AddRange(ReadFieldErrors(item));
                }

                break;
            case JsonValueKind.Object:
                string? message = GetString(element, "message");
                if (message is not null)
                {
                    string? fieldName = GetString(element, "fieldname") ?? GetString(element, "field_name") ?? GetString(element, "field");
                    result.Add(new FieldValidationError(fieldName, message));
                    break;
                }

                foreach (JsonProperty property in element.EnumerateObject())
                {
                    switch (property.Value.ValueKind)
                    {
                        case JsonValueKind.String:
                            result.Add(new FieldValidationError(property.Name, property.Value.GetString() ?? string.Empty));
                            break;
                        case JsonValueKind.Array:
                            foreach (JsonElement item in property.Value.EnumerateArray())
                            {
                                result.Add(new FieldValidationError(property.Name, item.ValueKind == JsonValueKind.String ? item.GetString() ?? string.Empty : item.GetRawText()));
                            }

                            break;
                        default:
                            result.Add(new FieldValidationError(property.Name, property.Value.GetRawText()));
                            break;
                    }
                }

                break;
            default:
                break;
        }

        return result;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => value.GetRawText(),
        };
    }
}
