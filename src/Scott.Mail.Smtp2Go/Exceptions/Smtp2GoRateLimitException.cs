namespace Scott.Mail.Smtp2Go;

/// <summary>The API answered 429. <see cref="RetryAfter"/> is taken from the <c>Retry-After</c> header when present.</summary>
public sealed class Smtp2GoRateLimitException : Smtp2GoApiException
{
    /// <summary>Initializes a new instance.</summary>
    public Smtp2GoRateLimitException()
    {
    }

    /// <summary>Initializes a new instance with a message.</summary>
    public Smtp2GoRateLimitException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and an inner exception.</summary>
    public Smtp2GoRateLimitException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance from a mapped response.</summary>
    public Smtp2GoRateLimitException(string message, int statusCode, string? path, ApiError? error, TimeSpan? retryAfter, Exception? innerException = null)
        : base(message, statusCode, path, error, innerException)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>How long the server asked the client to wait, or <see langword="null"/> when no <c>Retry-After</c> header was sent.</summary>
    public TimeSpan? RetryAfter { get; }
}
