namespace Scott.Mail.Smtp2Go;

/// <summary>The API answered 403 or <c>ENDPOINT_PERMISSION_DENIED</c>: the key exists but is not allowed to call <see cref="Smtp2GoApiException.Path"/>.</summary>
public sealed class Smtp2GoPermissionException : Smtp2GoApiException
{
    /// <summary>Initializes a new instance.</summary>
    public Smtp2GoPermissionException()
    {
    }

    /// <summary>Initializes a new instance with a message.</summary>
    public Smtp2GoPermissionException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and an inner exception.</summary>
    public Smtp2GoPermissionException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance from a mapped response.</summary>
    public Smtp2GoPermissionException(string message, int statusCode, string? path, ApiError? error, Exception? innerException = null)
        : base(message, statusCode, path, error, innerException)
    {
    }
}
