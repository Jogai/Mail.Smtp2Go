namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The error payload of a non-success response, whether it arrived nested (<c>{request_id, data: {error, error_code, field_validation_errors}}</c>),
/// flat (<c>{error, error_code}</c>) or unparseable (only <see cref="RawBody"/> is set).
/// </summary>
public sealed record ApiError
{
    /// <summary>The server-assigned request id, when the body carried one.</summary>
    public string? RequestId { get; init; }

    /// <summary>The <c>error</c> text.</summary>
    public string? Message { get; init; }

    /// <summary>The <c>error_code</c> string exactly as sent, for example <c>E_ApiResponseCodes.ENDPOINT_PERMISSION_DENIED</c>.</summary>
    public string? ErrorCode { get; init; }

    /// <summary><see cref="ErrorCode"/> parsed into the known set; <see cref="Smtp2GoErrorCode.Unknown"/> for anything else.</summary>
    public Smtp2GoErrorCode Code { get; init; } = Smtp2GoErrorCode.Unknown;

    /// <summary>The <c>field_validation_errors</c> entries, empty when absent.</summary>
    public IReadOnlyList<FieldValidationError> FieldValidationErrors { get; init; } = [];

    /// <summary>The response body as received.</summary>
    public string? RawBody { get; init; }

    /// <summary>Whether at least one of <see cref="Message"/>, <see cref="ErrorCode"/> or <see cref="FieldValidationErrors"/> was found in the body.</summary>
    public bool IsParsed => Message is not null || ErrorCode is not null || FieldValidationErrors.Count > 0;
}
