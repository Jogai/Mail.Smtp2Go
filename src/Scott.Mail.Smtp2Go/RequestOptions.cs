namespace Scott.Mail.Smtp2Go;

/// <summary>Per-call overrides. Every value is optional; unset values fall back to <see cref="Smtp2GoClientOptions"/>.</summary>
public sealed record RequestOptions
{
    /// <summary>
    /// Subaccount to act on. Merged into the request body as <c>subaccount_id</c> when the endpoint documents that field;
    /// ignored (with a diagnostic) for endpoints that do not.
    /// </summary>
    public string? SubaccountId { get; init; }

    /// <summary>API key to use for this call only, for example a master key for one call and a subaccount key for the next.</summary>
    public string? ApiKeyOverride { get; init; }

    /// <summary>Region for this call only. Takes precedence over <see cref="Smtp2GoClientOptions.Region"/> and <see cref="Smtp2GoClientOptions.BaseUrl"/>.</summary>
    public Region? Region { get; init; }

    /// <summary>Timeout for this call only. Takes precedence over <see cref="Smtp2GoClientOptions.Timeout"/>.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Allows a resilience pipeline to retry this call even though the endpoint is not idempotent (for example a send the caller has made idempotent).
    /// The core client does not retry by itself; the value is placed in the request options for the pipeline configured by the dependency injection package.
    /// </summary>
    public bool AllowRetry { get; init; }
}
