using System.Text.Json.Serialization.Metadata;

namespace Scott.Mail.Smtp2Go.DependencyInjection;

/// <summary>
/// Settings for a registered SMTP2GO client: every <see cref="Smtp2GoClientOptions"/> property under the same name (so one configuration section binds both)
/// plus <see cref="Resilience"/>. Bound from configuration by <c>AddSmtp2Go</c> and validated on start-up by <see cref="Smtp2GoOptionsValidator"/>.
/// </summary>
public sealed class Smtp2GoOptions
{
    /// <summary>The API key. Required. Sent in a header, never in the request body, and never logged.</summary>
    public string? ApiKey { get; set; }

    /// <summary>How the key is presented. Defaults to <see cref="Smtp2Go.AuthenticationScheme.ApiKeyHeader"/>.</summary>
    public AuthenticationScheme AuthenticationScheme { get; set; } = AuthenticationScheme.ApiKeyHeader;

    /// <summary>Regional endpoint. When set it takes precedence over <see cref="BaseUrl"/>.</summary>
    public Region? Region { get; set; }

    /// <summary>Custom absolute base URL (for example a proxy). Used when <see cref="Region"/> is not set. Defaults to <see cref="RegionEndpoints.Global"/>.</summary>
    public Uri? BaseUrl { get; set; }

    /// <summary>
    /// Per-request timeout applied by the core client through a cancellation token, independent of the resilience pipeline's
    /// <see cref="ResilienceOptions.TotalTimeout"/>; the shorter of the two wins. Defaults to 100 seconds; <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> disables it.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100);

    /// <summary>Default for the <c>fastaccept</c> flag on send requests that do not set it. <see langword="null"/> leaves the server default.</summary>
    public bool? DefaultFastAccept { get; set; }

    /// <summary>Subaccount applied to every call that does not set <see cref="RequestOptions.SubaccountId"/>, on endpoints that document <c>subaccount_id</c>.</summary>
    public string? DefaultSubaccountId { get; set; }

    /// <summary>Validate request bodies (size limits, recipient counts) before any network activity. Defaults to <see langword="true"/>.</summary>
    public bool ClientSideValidation { get; set; } = true;

    /// <summary>
    /// Whether the core client throttles rate-limited endpoints itself with its own token buckets. Defaults to <see langword="false"/> here, unlike the core
    /// default, because <see cref="ResilienceOptions.RateLimiting"/> already throttles every registered client's <c>HttpClient</c> pipeline; turn it on only
    /// when <see cref="RateLimitingOptions.Enabled"/> is off and the documented limits still have to be respected.
    /// </summary>
    public bool ClientSideRateLimiting { get; set; }

    /// <summary>
    /// An extra source-generated <see cref="System.Text.Json.Serialization.JsonSerializerContext"/> (or any resolver) whose types become usable with the raw
    /// client's typed overload. Not bindable from configuration; set it through the <c>Action&lt;Smtp2GoOptions&gt;</c> overload or <c>services.Configure</c>.
    /// </summary>
    public IJsonTypeInfoResolver? AdditionalJsonTypeInfoResolver { get; set; }

    /// <summary>Retry, timeout, circuit-breaker and rate-limiting settings of the <c>HttpClient</c> pipeline. Every property is read by the pipeline.</summary>
    public ResilienceOptions Resilience { get; } = new();

    /// <summary>Copies the client settings into the core <see cref="Smtp2GoClientOptions"/> the <see cref="Smtp2GoClient"/> constructor takes.</summary>
    public Smtp2GoClientOptions ToClientOptions()
    {
        return new Smtp2GoClientOptions
        {
            ApiKey = ApiKey,
            AuthenticationScheme = AuthenticationScheme,
            Region = Region,
            BaseUrl = BaseUrl,
            Timeout = Timeout,
            DefaultFastAccept = DefaultFastAccept,
            DefaultSubaccountId = DefaultSubaccountId,
            ClientSideValidation = ClientSideValidation,
            ClientSideRateLimiting = ClientSideRateLimiting,
            AdditionalJsonTypeInfoResolver = AdditionalJsonTypeInfoResolver,
        };
    }
}
