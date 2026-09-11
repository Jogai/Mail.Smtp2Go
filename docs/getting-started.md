# Getting started

Install the core package and construct a client with an API key. `client.Email` sends email (see [sending](sending.md)); `client.Webhooks`, `client.Stats`, `client.Activity`, `client.Templates`, `client.Suppressions` and `client.Archive` cover webhook management and reporting (see [webhooks](webhooks.md), [reporting](reporting.md) and [archive](archive.md)); the account-management and SMS families land with plan 07, and until then every endpoint is reachable through `client.Raw`, which applies the same authentication, regional routing, validation, error mapping and diagnostics the typed clients use.

```shell
dotnet package add Scott.Mail.Smtp2Go
```

## Construct a client

```csharp
using Scott.Mail.Smtp2Go;

// Simplest: one process-wide HttpClient is created lazily and shared by every client built this way.
var client = new Smtp2GoClient("api-...");

// With options: region, authentication scheme, timeout, default subaccount.
var regional = new Smtp2GoClient(new Smtp2GoClientOptions
{
    ApiKey = "api-...",
    Region = Region.EU,
    AuthenticationScheme = AuthenticationScheme.Bearer,
    Timeout = TimeSpan.FromSeconds(30),
});

// With your own HttpClient (never mutated; base address and headers are set per request).
var owned = new Smtp2GoClient(httpClient, new Smtp2GoClientOptions { ApiKey = "api-..." });
```

In a hosted application prefer the `Scott.Mail.Smtp2Go.DependencyInjection` package (plan 05), which supplies factory-managed `HttpClient` instances, options binding and a resilience pipeline.

## Call an endpoint through `Raw`

`stats/email_cycle` takes an empty body and returns the current billing cycle. Every response is an `ApiResponse<TData>` with the server's `RequestId`, the `Data` payload and an `Extra` bag holding any top-level field the library does not model.

```csharp
using System.Text.Json;

ApiResponse<JsonElement> cycle = await client.Raw.SendAsync<JsonElement, JsonElement>("stats/email_cycle", default);
Console.WriteLine($"request {cycle.RequestId}: {cycle.Data.GetProperty("cycle_max")} emails per cycle");

// The JsonDocument overload returns the whole envelope for undocumented endpoints or fields.
using JsonDocument raw = await client.Raw.SendJsonAsync("stats/email_cycle", default);
Console.WriteLine(raw.RootElement.GetProperty("request_id"));
```

Passing a body: any `JsonElement`, `JsonNode`/`JsonObject`/`JsonArray`, or a library model. To send your own record types register them in a `JsonSerializerContext` (with `ApiResponse<YourData>`) and assign it to `Smtp2GoClientOptions.AdditionalJsonTypeInfoResolver`; the library never falls back to reflection, so it stays trim- and AOT-safe.

```csharp
using var body = JsonDocument.Parse("""{"username":"alice@example.com"}""");
var summary = await client.Raw.SendAsync<JsonElement, JsonElement>("stats/email_summary", body.RootElement.Clone());
```

## Per-call overrides

`RequestOptions` overrides the client options for one call: `SubaccountId` (merged into the body as `subaccount_id` on endpoints that document it), `ApiKeyOverride`, `Region`, `Timeout` and `AllowRetry`.

```csharp
var eu = await client.Raw.SendAsync<JsonElement, JsonElement>(
    "webhook/view", default,
    options: new RequestOptions { Region = Region.EU, SubaccountId = "sub-123" });
```

## What can go wrong

Client-side validation (body size, documented limits) throws `Smtp2GoValidationException` before anything is sent. Non-success responses throw `Smtp2GoApiException` or one of its subtypes, carrying the status, `request_id`, `error_code` and any `field_validation_errors`. Transport failures, timeouts and cancellation surface as the framework's own `HttpRequestException`, `TimeoutException` and `OperationCanceledException`. See [errors](errors.md).
