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
| `ClientSideRateLimiting` | `true` | A token bucket per `RateLimitClass` (60/min for `activity/search`) delays calls that would exceed the documented limit, so paging helpers never trip it. Turn off when a resilience pipeline throttles instead. |
| `AdditionalJsonTypeInfoResolver` | unset | A source-generated `JsonSerializerContext` registering your own types (and `ApiResponse<T>` for them) for `IRawClient.SendAsync<TRequest, TResponse>`. |

`Validate()` throws `Smtp2GoValidationException` listing every problem; `GetValidationErrors()` and `GetValidationWarnings()` return them without throwing, for options validators.

## `RequestOptions`

Per-call overrides: `SubaccountId`, `ApiKeyOverride`, `Region`, `Timeout`, `AllowRetry`. `AllowRetry` marks a non-idempotent call as safe to retry for the resilience pipeline of the dependency injection package; the core client never retries on its own.

## Endpoint descriptors

`Scott.Mail.Smtp2Go.Transport.EndpointTable` holds one `Endpoint` per known path: HTTP method, idempotency, whether `subaccount_id` is accepted, the documented `RateLimitClass` and the body limit. Unknown paths get a conservative default (`POST`, not idempotent, no subaccount, 1 MB, or 50 MB under `email/`). The descriptor travels on each `HttpRequestMessage` under `RequestOptionKeys.EndpointKey` so a `DelegatingHandler` pipeline can read it with `RequestOptionKeys.TryGetEndpoint(request, out var endpoint)`.

## Diagnostics and tracing

Pass an `ISmtp2GoDiagnostics` to the `HttpClient` constructor to receive `RequestStarting`, `RequestCompleted`, `RequestFailed`, `ValidationFailed` and `SubaccountIdIgnored` notifications (the dependency injection package supplies an `ILogger` implementation). Independently, every call is one `Activity` of kind `Client` from the source `Smtp2GoActivitySource.Name` (`Scott.Mail.Smtp2Go`), tagged `smtp2go.endpoint`, `smtp2go.region`, `http.request.method`, `http.response.status_code` and `smtp2go.request_id`; subscribe with OpenTelemetry's `AddSource("Scott.Mail.Smtp2Go")`.

## Dependency injection

`AddSmtp2Go(...)` from `Scott.Mail.Smtp2Go.DependencyInjection` is written in plan 05.
