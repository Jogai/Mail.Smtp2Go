namespace Scott.Mail.Smtp2Go.Transport;

/// <summary>
/// Describes one SMTP2GO API endpoint. The transport reads it to decide the HTTP method, whether <c>subaccount_id</c> may be merged
/// into the body, the body-size limit and (through <see cref="RequestOptionKeys"/>) what a resilience pipeline may retry or throttle.
/// </summary>
/// <param name="Path">Path relative to the v3 base URL, for example <c>email/send</c>. No leading slash.</param>
/// <param name="Method">HTTP method. <c>POST</c> for every endpoint except the three <c>patch</c> endpoints.</param>
/// <param name="Idempotent">Whether the call can be repeated safely (view, search and summary endpoints).</param>
/// <param name="AcceptsSubaccountId">Whether the endpoint documents a <c>subaccount_id</c> body field.</param>
/// <param name="RateLimit">The documented rate-limit class.</param>
/// <param name="MaxBodyBytes">Maximum request body size in bytes: 50 MB for <c>email/*</c>, 1 MB otherwise.</param>
public sealed record Endpoint(
    string Path,
    HttpMethod Method,
    bool Idempotent,
    bool AcceptsSubaccountId,
    RateLimitClass RateLimit,
    long MaxBodyBytes)
{
    /// <summary>One megabyte, the documented limit for every endpoint outside <c>email/*</c>.</summary>
    public const long DefaultMaxBodyBytes = 1L * 1024 * 1024;

    /// <summary>Fifty megabytes, the documented limit for <c>email/*</c>.</summary>
    public const long EmailMaxBodyBytes = 50L * 1024 * 1024;

    /// <summary><c>PATCH</c>, which <see cref="HttpMethod"/> does not expose on netstandard2.0.</summary>
    public static HttpMethod Patch { get; } = new("PATCH");
}
