namespace Scott.Mail.Smtp2Go.DependencyInjection;

/// <summary>
/// Stable event ids of every log message the package writes: 100s for requests, 200s for failures, 300s for validation, 400s for resilience.
/// Categories are <see cref="LoggerDiagnostics.CategoryName"/> (<c>Scott.Mail.Smtp2Go</c>) and <c>Scott.Mail.Smtp2Go.Resilience</c>.
/// </summary>
public static class Smtp2GoEventIds
{
    /// <summary>Debug: a request is about to be sent.</summary>
    public const int RequestStarting = 100;

    /// <summary>Information: a request completed with a success status.</summary>
    public const int RequestCompleted = 101;

    /// <summary>Information: a send call reported accepted and rejected recipient counts.</summary>
    public const int EmailResult = 102;

    /// <summary>Warning: a request failed with an API error or a transport exception.</summary>
    public const int RequestFailed = 200;

    /// <summary>Warning: client-side validation rejected a request before it was sent.</summary>
    public const int ValidationFailed = 300;

    /// <summary>Debug: a subaccount id was ignored because the endpoint does not accept one.</summary>
    public const int SubaccountIdIgnored = 301;

    /// <summary>Information: an attempt failed and will be retried.</summary>
    public const int Retrying = 400;

    /// <summary>Warning: the circuit breaker opened.</summary>
    public const int CircuitOpened = 401;

    /// <summary>Information: the circuit breaker closed again.</summary>
    public const int CircuitClosed = 402;

    /// <summary>Debug: the circuit breaker is letting a probe request through.</summary>
    public const int CircuitHalfOpened = 403;

    /// <summary>Warning: the client-side rate limiter rejected a request because its queue was full.</summary>
    public const int RateLimitRejected = 404;

    /// <summary>Warning: an attempt or the whole call timed out in the resilience pipeline.</summary>
    public const int PipelineTimeout = 405;
}
