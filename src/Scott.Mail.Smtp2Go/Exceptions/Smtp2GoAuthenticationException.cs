namespace Scott.Mail.Smtp2Go;

/// <summary>The API answered 401: the key is missing, malformed or revoked.</summary>
public sealed class Smtp2GoAuthenticationException : Smtp2GoApiException
{
    /// <summary>Initializes a new instance.</summary>
    public Smtp2GoAuthenticationException()
    {
    }

    /// <summary>Initializes a new instance with a message.</summary>
    public Smtp2GoAuthenticationException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and an inner exception.</summary>
    public Smtp2GoAuthenticationException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance from a mapped response.</summary>
    public Smtp2GoAuthenticationException(string message, int statusCode, string? path, ApiError? error, Exception? innerException = null)
        : base(message, statusCode, path, error, innerException)
    {
    }
}
