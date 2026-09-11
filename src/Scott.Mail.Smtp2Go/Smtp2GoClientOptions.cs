using System.Text.Json.Serialization.Metadata;

namespace Scott.Mail.Smtp2Go;

/// <summary>Settings for the SMTP2GO client. A plain mutable class so it can be bound from configuration.</summary>
public sealed class Smtp2GoClientOptions
{
    private const int ApiKeyLength = 36;
    private const string ApiKeyPrefix = "api-";

    /// <summary>The API key. Required. Sent in a header, never in the request body, and never logged.</summary>
    public string? ApiKey { get; set; }

    /// <summary>How the key is presented. Defaults to <see cref="AuthenticationScheme.ApiKeyHeader"/>.</summary>
    public AuthenticationScheme AuthenticationScheme { get; set; } = AuthenticationScheme.ApiKeyHeader;

    /// <summary>Regional endpoint. When set it takes precedence over <see cref="BaseUrl"/>.</summary>
    public Region? Region { get; set; }

    /// <summary>Custom absolute base URL (for example a proxy). Used when <see cref="Region"/> is not set. Defaults to <see cref="RegionEndpoints.Global"/>.</summary>
    public Uri? BaseUrl { get; set; }

    /// <summary>Per-request timeout, applied through a linked cancellation token so a caller-supplied <see cref="HttpClient"/> is never mutated. Defaults to 100 seconds; <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> disables it.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100);

    /// <summary>Default for the <c>fastaccept</c> flag on send requests that do not set it. <see langword="null"/> leaves the server default.</summary>
    public bool? DefaultFastAccept { get; set; }

    /// <summary>Subaccount applied to every call that does not set <see cref="RequestOptions.SubaccountId"/>, on endpoints that document <c>subaccount_id</c>.</summary>
    public string? DefaultSubaccountId { get; set; }

    /// <summary>Validate request bodies (size limits, recipient counts) before any network activity. Defaults to <see langword="true"/>.</summary>
    public bool ClientSideValidation { get; set; } = true;

    /// <summary>
    /// Whether the client throttles calls to rate-limited endpoints itself (a token bucket per <see cref="Transport.RateLimitClass"/>, for example 60 per minute for
    /// <c>activity/search</c>) so that paging helpers never trip the documented limits. Default <see langword="true"/>. Turn it off when a resilience pipeline
    /// (the dependency injection package) does the throttling.
    /// </summary>
    public bool ClientSideRateLimiting { get; set; } = true;

    /// <summary>
    /// An extra source-generated <see cref="System.Text.Json.Serialization.JsonSerializerContext"/> (or any resolver) whose types become usable with the raw
    /// client's typed overload, for endpoints this library has no model for. Register both the request type and <c>ApiResponse&lt;TResponse&gt;</c> in it.
    /// The library's own context is always consulted first.
    /// </summary>
    public IJsonTypeInfoResolver? AdditionalJsonTypeInfoResolver { get; set; }

    /// <summary>Throws <see cref="Smtp2GoValidationException"/> listing every problem found by <see cref="GetValidationErrors"/>.</summary>
    public void Validate()
    {
        IReadOnlyList<string> errors = GetValidationErrors();
        if (errors.Count > 0)
        {
            throw new Smtp2GoValidationException("Smtp2GoClientOptions is not valid.", errors);
        }
    }

    /// <summary>Returns every problem that makes these options unusable. Empty when the options are valid.</summary>
    public IReadOnlyList<string> GetValidationErrors()
    {
        List<string> errors = [];
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            errors.Add("ApiKey is required.");
        }

        if (BaseUrl is not null && (!BaseUrl.IsAbsoluteUri || (BaseUrl.Scheme != Uri.UriSchemeHttp && BaseUrl.Scheme != Uri.UriSchemeHttps)))
        {
            errors.Add("BaseUrl must be an absolute http or https URL.");
        }

        if (Timeout <= TimeSpan.Zero && Timeout != System.Threading.Timeout.InfiniteTimeSpan)
        {
            errors.Add("Timeout must be positive or Timeout.InfiniteTimeSpan.");
        }

        return errors;
    }

    /// <summary>
    /// Returns advisory problems that do not stop the client from being used, such as an API key that does not match the documented
    /// <c>api-</c> plus 32 alphanumeric characters format (sandbox keys may differ).
    /// </summary>
    public IReadOnlyList<string> GetValidationWarnings()
    {
        List<string> warnings = [];
        if (!string.IsNullOrWhiteSpace(ApiKey) && !IsWellFormedApiKey(ApiKey!))
        {
            warnings.Add("ApiKey does not match the documented format 'api-' followed by 32 alphanumeric characters.");
        }

        return warnings;
    }

    /// <summary>Whether <paramref name="apiKey"/> matches the documented <c>^api-[A-Za-z0-9]{32}$</c> format.</summary>
    public static bool IsWellFormedApiKey(string apiKey)
    {
        Argument.ThrowIfNull(apiKey);
        if (apiKey.Length != ApiKeyLength || !apiKey.StartsWith(ApiKeyPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        for (int i = ApiKeyPrefix.Length; i < apiKey.Length; i++)
        {
            char c = apiKey[i];
            bool alphanumeric = (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
            if (!alphanumeric)
            {
                return false;
            }
        }

        return true;
    }
}
