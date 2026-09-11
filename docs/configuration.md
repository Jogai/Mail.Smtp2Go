# Configuration

## `Smtp2GoClientOptions`

| Property | Default | Notes |
| :-- | :-- | :-- |
| `ApiKey` | none, required | Sent in a header, never in the body or logs. `Validate()` fails without it; a key that does not match `api-` plus 32 alphanumerics is only a warning (`GetValidationWarnings()`), because sandbox keys may differ. |
| `AuthenticationScheme` | `ApiKeyHeader` | `X-Smtp2go-Api-Key: <key>`. `Bearer` sends `Authorization: Bearer <key>` instead. |
| `Region` | unset | `Global`, `US`, `EU`, `AU`. Precedence per call: `RequestOptions.Region`, then this, then `BaseUrl`, then `https://api.smtp2go.com/v3/`. Constants in `RegionEndpoints`. |
| `BaseUrl` | unset | Absolute `http(s)` URL, for a proxy or test server. A trailing slash is added. |
| `Timeout` | 100 s | Per request, through a linked cancellation token; `Timeout.InfiniteTimeSpan` disables it. Never applied to a caller-supplied `HttpClient`. |
| `DefaultFastAccept` | unset | Applied to send requests that do not set `fastaccept` (plan 03). |
| `DefaultSubaccountId` | unset | Merged as `subaccount_id` into every call on an endpoint that documents it; `RequestOptions.SubaccountId` wins per call. Endpoints that do not accept it ignore the value and raise `ISmtp2GoDiagnostics.SubaccountIdIgnored`. |
| `ClientSideValidation` | `true` | Body-size limits (50 MB under `email/*`, 1 MB elsewhere) and request-model checks run before sending. |
| `ClientSideRateLimiting` | `false` | The core client's own per-endpoint token buckets. Off by default here because `Resilience:RateLimiting` throttles the pipeline; turn it on only when that limiter is disabled. |
| `AdditionalJsonTypeInfoResolver` | unset | A source-generated `JsonSerializerContext` registering your own types (and `ApiResponse<T>` for them) for `IRawClient.SendAsync<TRequest, TResponse>`. |

`Validate()` throws `Smtp2GoValidationException` listing every problem; `GetValidationErrors()` and `GetValidationWarnings()` return them without throwing, for options validators.

## `RequestOptions`

Per-call overrides: `SubaccountId`, `ApiKeyOverride`, `Region`, `Timeout`, `AllowRetry`. `AllowRetry` marks a non-idempotent call as safe to retry for the resilience pipeline of the dependency injection package; the core client never retries on its own.

## Endpoint descriptors

`Scott.Mail.Smtp2Go.Transport.EndpointTable` holds one `Endpoint` per known path: HTTP method, idempotency, whether `subaccount_id` is accepted, the documented `RateLimitClass` and the body limit. Unknown paths get a conservative default (`POST`, not idempotent, no subaccount, 1 MB, or 50 MB under `email/`). The descriptor travels on each `HttpRequestMessage` under `RequestOptionKeys.EndpointKey` so a `DelegatingHandler` pipeline can read it with `RequestOptionKeys.TryGetEndpoint(request, out var endpoint)`.

## Diagnostics and tracing

Pass an `ISmtp2GoDiagnostics` to the `HttpClient` constructor to receive `RequestStarting`, `RequestCompleted`, `RequestFailed`, `ValidationFailed`, `SubaccountIdIgnored` and `EmailResult` notifications (the dependency injection package supplies an `ILogger` implementation). Independently, every call is one `Activity` of kind `Client` from the source `Smtp2GoActivitySource.Name` (`Scott.Mail.Smtp2Go`), tagged `smtp2go.endpoint`, `smtp2go.region`, `http.request.method`, `http.response.status_code` and `smtp2go.request_id`; subscribe with OpenTelemetry's `AddSource("Scott.Mail.Smtp2Go")`. Logs, event ids and metrics are described in [observability](observability.md).

## Dependency injection

`Scott.Mail.Smtp2Go.DependencyInjection` registers a validated, factory-managed, resilient client in one call. It targets `net8.0` and `net10.0`.

```shell
dotnet package add Scott.Mail.Smtp2Go.DependencyInjection
```

```csharp
// From configuration (appsettings.json, user secrets, environment variables):
builder.Services.AddSmtp2Go(builder.Configuration.GetSection("Smtp2Go"));

// In code:
builder.Services.AddSmtp2Go(o =>
{
    o.ApiKey = "api-...";
    o.Region = Region.EU;
    o.DefaultFastAccept = true;
    o.Resilience.MaxRetries = 5;
});

// A second, named client with its own key:
builder.Services.AddSmtp2Go("marketing", builder.Configuration.GetSection("Smtp2Go:Marketing"));
```

Each overload returns an `ISmtp2GoBuilder` with `Name`, `Services` and `HttpClientBuilder`, the `IHttpClientBuilder` of the named `HttpClient` (`Scott.Mail.Smtp2Go`, or `Scott.Mail.Smtp2Go:marketing`), so you can add message handlers, change the primary handler or the handler lifetime:

```csharp
builder.Services.AddSmtp2Go(builder.Configuration.GetSection("Smtp2Go"))
    .HttpClientBuilder.AddHttpMessageHandler<MyAuditHandler>();
```

What `AddSmtp2Go` registers:

| Service | Lifetime | Notes |
| :-- | :-- | :-- |
| `IOptions<Smtp2GoOptions>` / `IOptionsMonitor<Smtp2GoOptions>` | | Named per client; `ValidateOnStart` with `Smtp2GoOptionsValidator`. |
| `ISmtp2GoClient` | transient | Only for the unnamed registration; created through the factory. |
| `ISmtp2GoClient` keyed by name | transient | For every registration: `[FromKeyedServices("marketing")] ISmtp2GoClient client` or `provider.GetRequiredKeyedService<ISmtp2GoClient>("marketing")`. |
| `ISmtp2GoClientFactory` | singleton | `Create(name)`; use `Options.DefaultName` (`""`) for the unnamed client. |
| `ISmtp2GoDiagnostics` | singleton | `LoggerDiagnostics`, logging and metrics. Register your own before `AddSmtp2Go` to replace it (`TryAdd` semantics). |
| `Smtp2GoMetrics` | singleton | The `Scott.Mail.Smtp2Go` meter. |
| `HttpClient` named `Scott.Mail.Smtp2Go[:name]` | | `Timeout` infinite (the pipeline owns timeouts), HTTP/2 preferred with downgrade, the resilience handler attached. |

Clients are cheap: resolve one per unit of work rather than caching it, so `IHttpClientFactory` handler rotation and options reloads take effect.

### `Smtp2GoOptions`

`Smtp2GoOptions` has every `Smtp2GoClientOptions` property under the same name (the table above applies unchanged, so one section binds both) plus `Resilience`. `AdditionalJsonTypeInfoResolver` cannot come from configuration; set it in the `Action<Smtp2GoOptions>` overload or with `services.Configure<Smtp2GoOptions>(...)`. Binding is source generated, so the package stays trim- and AOT-safe.

Validation runs when the host starts and every message names the configuration key, for example `Smtp2Go:ApiKey is required.` or `Smtp2Go:Marketing:Resilience:CircuitBreaker:FailureRatio must be greater than 0 and at most 1.`; for options configured in code the prefix is `Smtp2Go` or `Smtp2Go:<name>`.

```json
{
  "Smtp2Go": {
    "ApiKey": "api-...",
    "Region": "EU",
    "DefaultFastAccept": true,
    "Timeout": "00:00:30",
    "Resilience": {
      "MaxRetries": 3,
      "RetryBaseDelay": "00:00:01",
      "RetryOnSendEndpoints": false,
      "AttemptTimeout": "00:00:30",
      "TotalTimeout": "00:01:40",
      "CircuitBreaker": { "Enabled": true, "FailureRatio": 0.5, "MinimumThroughput": 20, "SamplingDuration": "00:00:30", "BreakDuration": "00:00:30" },
      "RateLimiting": {
        "Enabled": true,
        "GlobalConcurrency": 32,
        "GlobalQueueLimit": 256,
        "Overrides": { "ActivitySearch": { "PermitLimit": 120, "Window": "00:01:00", "QueueLimit": 1024 } }
      }
    },
    "Marketing": { "ApiKey": "api-...", "Region": "US" }
  }
}
```

Keep the key out of source control: `dotnet user-secrets set "Smtp2Go:ApiKey" "api-..."` locally, and the `Smtp2Go__ApiKey` environment variable or your secret store in deployment.

### Resilience

The `HttpClient` gets a `Microsoft.Extensions.Http.Resilience` pipeline built from `Smtp2GoOptions.Resilience`. Strategies run in this order, outermost first: rate limiter, total timeout, retry, circuit breaker, attempt timeout. Every property below is read by the pipeline; a unit test fails if one stops being consumed.

| Property | Default | What it does |
| :-- | :-- | :-- |
| `MaxRetries` | 3 | Retries after the first attempt; `0` removes the retry strategy. |
| `RetryBaseDelay` | 1 s | Base of the exponential back-off with jitter (about 1 s, 2 s, 4 s). A `Retry-After` header on a 429 replaces the computed delay. |
| `RetryOnSendEndpoints` | `false` | Also retry endpoints that are not idempotent (`email/send`, `email/mime`, `email/batch`, ...). See below. |
| `AttemptTimeout` | 30 s | Per attempt. A timed-out attempt counts as a transient failure and is retried. |
| `TotalTimeout` | 100 s | Across all attempts, including retry delays and rate-limiter waits. Fails with `TimeoutRejectedException`. |
| `CircuitBreaker.Enabled` | `true` | Removes the breaker when `false`. |
| `CircuitBreaker.FailureRatio` | 0.5 | Share of failed calls, in (0, 1], that opens the circuit. |
| `CircuitBreaker.MinimumThroughput` | 20 | Calls needed within `SamplingDuration` before the ratio counts (at least 2). |
| `CircuitBreaker.SamplingDuration` | 30 s | Failure-counting window (at least 500 ms). |
| `CircuitBreaker.BreakDuration` | 30 s | How long calls fail fast with `BrokenCircuitException` before a probe is let through (at least 500 ms). |
| `RateLimiting.Enabled` | `true` | Removes both limiters when `false`. |
| `RateLimiting.GlobalConcurrency` | 32 | Requests in flight at once, per registered client. |
| `RateLimiting.GlobalQueueLimit` | 256 | Requests waiting for a concurrency permit before new ones are rejected with `RateLimiterRejectedException`. |
| `RateLimiting.Overrides` | empty | Replaces a documented window per `RateLimitClass`: `PermitLimit`, `Window`, `QueueLimit` (default 1024). |

**Retry.** An attempt is retried on 429, 408, 500, 502, 503, 504, `HttpRequestException` and an attempt timeout; never on 400, 401, 402, 403 or 404, which are the caller's problem and would just repeat. But whether a call is retried at all depends on the endpoint: only endpoints whose descriptor says `Idempotent` (view, search and summary endpoints) are retried by default. A send that timed out may well have been accepted by SMTP2GO, and retrying it delivers the message twice, so `email/send`, `email/mime`, `email/batch` and every other mutating endpoint fail on the first transient error. Two ways to opt in: `Resilience.RetryOnSendEndpoints = true` for every call of the client, when duplicates are acceptable or you deduplicate downstream, or `new RequestOptions { AllowRetry = true }` on a single call you have made idempotent yourself. Requests that do not come from this library (no endpoint descriptor) are retried only for `GET`, `HEAD` and `OPTIONS`.

**Timeouts.** `Smtp2GoOptions.Timeout` (100 s) is applied by the core client through the cancellation token and surfaces as `TimeoutException`; the pipeline's `TotalTimeout` surfaces as `TimeoutRejectedException`. The shorter of the two wins, so set `RequestOptions.Timeout` per call when you need something tighter than both.

**Circuit breaker.** Counts 5xx responses, transport exceptions and attempt timeouts. 4xx responses, including 429, never trip it: a rate-limited or misconfigured call says nothing about the API's health. While open, every call throws `BrokenCircuitException` without a network round trip.

**Rate limiting.** SMTP2GO documents per-endpoint request rates (60/min `activity/search`, 5/min `api_keys/add`, 50/hour `subaccounts/add`, 20/min for the deprecated `email/search`). The pipeline enforces each as a sliding window on the client side, keyed by the endpoint's `RateLimitClass`, so a burst queues locally instead of collecting 429s; a request that would exceed the queue fails immediately with `RateLimiterRejectedException`. Every request additionally takes a permit from the concurrency limiter. Limiters are per registered client, which matches per-key limits on the API side. Raise a window with `Overrides` when SMTP2GO has raised your account's limit; `RateLimitClass.None` cannot be overridden.

Options reloads (`IOptionsMonitor`) rebuild the pipeline, so changing `Resilience` in configuration takes effect without a restart.
