# Observability

The core package emits one trace span per API call. The `Scott.Mail.Smtp2Go.DependencyInjection` package adds structured logs with stable event ids and a `Meter`. Nothing ever logs or tags the API key or a request or response body.

## Tracing

Every call is one `Activity` of kind `Client` from the `ActivitySource` named `Scott.Mail.Smtp2Go` (`Smtp2GoActivitySource.Name`), named after the endpoint path (for example `email/send`) and tagged:

| Tag | Value |
| :-- | :-- |
| `smtp2go.endpoint` | Endpoint path, `email/send` |
| `smtp2go.region` | `Global`, `US`, `EU`, `AU`, or `custom` for a `BaseUrl` |
| `http.request.method` | `POST` or `PATCH` |
| `http.response.status_code` | Status code, when a response was received |
| `smtp2go.request_id` | The API's `request_id`, when the body carried one |

The span status is `Ok` on success, `Error` with the exception message on failure and `Error` with `cancelled` on caller cancellation. Retries happen inside the span (the resilience pipeline runs below the transport), so one call is one span; `HttpClient`'s own `System.Net.Http` spans appear as children per attempt.

## Logs

Two categories, both written through `ILogger`:

| Category | Written by | What |
| :-- | :-- | :-- |
| `Scott.Mail.Smtp2Go` | `LoggerDiagnostics` (the `ISmtp2GoDiagnostics` registered by `AddSmtp2Go`) | Requests, failures, validation |
| `Scott.Mail.Smtp2Go.Resilience` | The resilience pipeline callbacks | Retries, circuit breaker, rate limiter, timeouts |

Event ids are constants on `Smtp2GoEventIds` and never change meaning:

| Id | Level | Category | Message |
| :-- | :-- | :-- | :-- |
| 100 | Debug | `Scott.Mail.Smtp2Go` | `SMTP2GO {Method} {Endpoint} starting (region {Region})` |
| 101 | Information | `Scott.Mail.Smtp2Go` | `SMTP2GO {Method} {Endpoint} returned {StatusCode} in {ElapsedMs} ms (request_id {RequestId})` |
| 102 | Information | `Scott.Mail.Smtp2Go` | `SMTP2GO send accepted {Succeeded} and rejected {Failed} recipients` |
| 200 | Warning | `Scott.Mail.Smtp2Go` | `SMTP2GO {Method} {Endpoint} failed with status {StatusCode} (request_id {RequestId})`, with the exception |
| 300 | Warning | `Scott.Mail.Smtp2Go` | `SMTP2GO {Endpoint} rejected by client-side validation with {ErrorCount} error(s): {Errors}` |
| 301 | Debug | `Scott.Mail.Smtp2Go` | `SMTP2GO {Endpoint} does not accept subaccount_id; the configured value was not sent` |
| 400 | Information | `Scott.Mail.Smtp2Go.Resilience` | `SMTP2GO {Endpoint} attempt {Attempt} failed ({Outcome}); retrying in {DelayMs} ms` |
| 401 | Warning | `Scott.Mail.Smtp2Go.Resilience` | `SMTP2GO circuit opened for {BreakDurationMs} ms after {Endpoint} failed ({Outcome})` |
| 402 | Information | `Scott.Mail.Smtp2Go.Resilience` | `SMTP2GO circuit closed after {Endpoint} succeeded` |
| 403 | Debug | `Scott.Mail.Smtp2Go.Resilience` | `SMTP2GO circuit half-open; letting one probe request through` |
| 404 | Warning | `Scott.Mail.Smtp2Go.Resilience` | `SMTP2GO {Endpoint} rejected by the client-side rate limiter ({RateLimitClass}); retry after {RetryAfterMs} ms` |
| 405 | Warning | `Scott.Mail.Smtp2Go.Resilience` | `SMTP2GO {Endpoint} {Scope} timed out after {TimeoutMs} ms` (`Scope` is `attempt` or `call`) |

`{Outcome}` is `HTTP <status>` or an exception type name (`HttpRequestException`, `TimeoutRejectedException`). A unit test scans every `[LoggerMessage]` template in the package and fails if a template or parameter names the key or a body.

The 100-range messages are per request. In production, set `"Scott.Mail.Smtp2Go": "Warning"` in `Logging:LogLevel` to keep only failures, or `"Information"` to keep one line per call with the `request_id` SMTP2GO support asks for.

Event 102 is raised through `ISmtp2GoDiagnostics.EmailResult(succeeded, failed)`, which the email client calls after every `SendAsync` and `SendMimeAsync` that returned recipient counts (a `fastaccept` response carries none, and `email/batch` items carry only ids, so neither raises it); it also drives the two email counters below.

## Metrics

`Smtp2GoMetrics` creates the `Meter` named `Scott.Mail.Smtp2Go` (`Smtp2GoMetrics.MeterName`) through `IMeterFactory`:

| Instrument | Type | Unit | Tags |
| :-- | :-- | :-- | :-- |
| `smtp2go.client.requests` | Counter | `{request}` | `smtp2go.endpoint`; `http.response.status_code` when a response arrived; `error.type` (exception type name) on failure |
| `smtp2go.client.request.duration` | Histogram | `s` | `smtp2go.endpoint`, `http.response.status_code`; recorded for successful calls |
| `smtp2go.email.accepted` | Counter | `{recipient}` | none; recipients the API accepted on send calls |
| `smtp2go.email.failed` | Counter | `{recipient}` | none; recipients the API rejected on send calls |

Every call increments `smtp2go.client.requests` exactly once, whether it succeeded, failed with an API error (then tagged with both the status and `error.type=Smtp2GoApiException`) or failed in transport. Retries are not separate requests at this level; the pipeline's own attempts show up in `HttpClient`'s `http.client.request.duration`.

## OpenTelemetry

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(Smtp2GoActivitySource.Name)     // "Scott.Mail.Smtp2Go"
        .AddHttpClientInstrumentation())
    .WithMetrics(metrics => metrics
        .AddMeter(Smtp2GoMetrics.MeterName)        // "Scott.Mail.Smtp2Go"
        .AddHttpClientInstrumentation());
```

Logs flow through the host's `ILogger` providers as usual; with `builder.Logging.AddOpenTelemetry(...)` the event ids and named placeholders above arrive as structured attributes.

## Without the dependency injection package

Pass your own `ISmtp2GoDiagnostics` to the `Smtp2GoClient(HttpClient, Smtp2GoClientOptions, ISmtp2GoDiagnostics)` constructor to receive `RequestStarting`, `RequestCompleted`, `RequestFailed`, `ValidationFailed`, `SubaccountIdIgnored` and `EmailResult`. The `ActivitySource` is always active; the `Meter` exists only through `Smtp2GoMetrics`, which you can construct with any `IMeterFactory` and wrap in your own diagnostics implementation.
