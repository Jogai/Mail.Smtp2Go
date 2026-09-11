using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.DependencyInjection;

/// <summary>
/// Settings of the resilience pipeline the dependency injection package adds to the client's <c>HttpClient</c>:
/// rate limiter, total timeout, retry, circuit breaker and attempt timeout, in that order. Every property is consumed by the pipeline.
/// </summary>
public sealed class ResilienceOptions
{
    /// <summary>Maximum number of retries after the first attempt. <c>0</c> disables the retry strategy. Defaults to 3.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Base delay of the exponential back-off with jitter between retries. Defaults to 1 second.</summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Also retry endpoints that are not idempotent, such as <c>email/send</c>. Off by default because a retried send can deliver the same
    /// message twice when the first attempt was accepted but the response was lost. Per call, <see cref="RequestOptions.AllowRetry"/> opts a single request in.
    /// </summary>
    public bool RetryOnSendEndpoints { get; set; }

    /// <summary>Timeout of one attempt. A timed-out attempt counts as a transient failure for retry and circuit breaker. Defaults to 30 seconds.</summary>
    public TimeSpan AttemptTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Timeout across all attempts of one call, including retry delays and rate-limiter waits. Defaults to 100 seconds.</summary>
    public TimeSpan TotalTimeout { get; set; } = TimeSpan.FromSeconds(100);

    /// <summary>Circuit breaker on 5xx responses and transport failures. 4xx responses never trip it.</summary>
    public CircuitBreakerOptions CircuitBreaker { get; } = new();

    /// <summary>Client-side request-rate limiting per <see cref="RateLimitClass"/> plus a global concurrency cap.</summary>
    public RateLimitingOptions RateLimiting { get; } = new();
}
