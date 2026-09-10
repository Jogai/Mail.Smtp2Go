namespace Scott.Mail.Smtp2Go;

/// <summary>The API answered with a non-success status. Carries the status, the parsed error payload and the raw body.</summary>
public class Smtp2GoApiException : Smtp2GoException
{
    /// <summary>Initializes a new instance.</summary>
    public Smtp2GoApiException()
    {
    }

    /// <summary>Initializes a new instance with a message.</summary>
    public Smtp2GoApiException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and an inner exception.</summary>
    public Smtp2GoApiException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance from a mapped response.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="path">The endpoint path that was called, for example <c>webhook/add</c>.</param>
    /// <param name="error">The parsed error payload, if any.</param>
    /// <param name="innerException">An optional inner exception.</param>
    public Smtp2GoApiException(string message, int statusCode, string? path, ApiError? error, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Path = path;
        Error = error;
    }

    /// <summary>The HTTP status code, or 0 when the exception did not come from a response.</summary>
    public int StatusCode { get; }

    /// <summary>The endpoint path that was called.</summary>
    public string? Path { get; }

    /// <summary>The parsed error payload, if any.</summary>
    public ApiError? Error { get; }

    /// <summary>The server-assigned request id, when the body carried one.</summary>
    public string? RequestId => Error?.RequestId;

    /// <summary>The <c>error_code</c> string exactly as sent.</summary>
    public string? ErrorCode => Error?.ErrorCode;

    /// <summary><see cref="ErrorCode"/> parsed into the known set.</summary>
    public Smtp2GoErrorCode Code => Error?.Code ?? Smtp2GoErrorCode.Unknown;

    /// <summary>The <c>field_validation_errors</c> entries, empty when absent.</summary>
    public IReadOnlyList<FieldValidationError> FieldValidationErrors => Error?.FieldValidationErrors ?? [];

    /// <summary>The response body as received.</summary>
    public string? RawBody => Error?.RawBody;
}
