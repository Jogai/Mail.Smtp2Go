using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.DependencyInjection;

/// <summary>
/// Client-side rate limiting: one sliding-window limiter per documented <see cref="RateLimitClass"/> (60/min for <c>activity/search</c>, 5/min for
/// <c>api_keys/add</c>, 50/hour for <c>subaccounts/add</c>, 20/min for <c>email/search</c>) and one concurrency limiter for every request.
/// Requests over a window limit queue until a permit frees up; requests over a queue limit fail with <c>RateLimiterRejectedException</c>.
/// </summary>
public sealed class RateLimitingOptions
{
    /// <summary>Whether the rate limiter is part of the pipeline. Defaults to <see langword="true"/>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Maximum number of requests in flight at once for this client. Defaults to 32.</summary>
    public int GlobalConcurrency { get; set; } = 32;

    /// <summary>Maximum number of requests waiting for a concurrency permit; more are rejected. Defaults to 256.</summary>
    public int GlobalQueueLimit { get; set; } = 256;

    /// <summary>
    /// Replaces the documented window of a <see cref="RateLimitClass"/>, for example when SMTP2GO has raised a limit for your account.
    /// Keys are class names (<c>ActivitySearch</c>, <c>ApiKeyAdd</c>, <c>SubaccountAdd</c>, <c>EmailSearch</c>). <see cref="RateLimitClass.None"/> cannot be limited.
    /// </summary>
    public Dictionary<RateLimitClass, RateLimitWindow> Overrides { get; } = [];
}
