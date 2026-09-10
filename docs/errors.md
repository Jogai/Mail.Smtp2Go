# Errors

Every exception the library throws derives from `Smtp2GoException`. Transport failures and cancellation are deliberately not wrapped so resilience pipelines and cancellation semantics keep working.

| Condition | Result |
| :-- | :-- |
| 2xx with a parseable envelope | The `ApiResponse<TData>` is returned. Send endpoints report per-recipient failures inside `Data` (plan 03 adds `EnsureAccepted()` for callers who want exception semantics). |
| 4xx/5xx with `{request_id, data: {error, error_code, field_validation_errors}}` | `Smtp2GoApiException` with `StatusCode`, `Path`, `RequestId`, `ErrorCode` (the raw string), `Code` (`Smtp2GoErrorCode`, `Unknown` for anything unrecognised), `Message` from `error`, `FieldValidationErrors` (`FieldName`, `Message` pairs; the object, array and map shapes are all accepted), `RawBody`. |
| 4xx/5xx with the flat `{error, error_code}` shape | Same exception; the parser tries `data.error` first, then the top level. |
| 401 | `Smtp2GoAuthenticationException`. |
| 403, or any status with `ENDPOINT_PERMISSION_DENIED` | `Smtp2GoPermissionException`; the message names the endpoint the key may not call. |
| 429 | `Smtp2GoRateLimitException` with `RetryAfter` parsed from the `Retry-After` header (seconds or HTTP date), `null` when absent. |
| Non-success with an unparseable body | `Smtp2GoApiException` with `RawBody` set, `ErrorCode` `null`, `Code` `Unknown`. |
| 2xx with an empty or malformed body | `Smtp2GoApiException` (`InnerException` is the `JsonException` when parsing failed). |
| Body over the endpoint's limit, or a request model's own checks | `Smtp2GoValidationException` with `Errors`, thrown before any network activity. Switch off with `Smtp2GoClientOptions.ClientSideValidation = false`. |
| `Smtp2GoClientOptions.Validate()` failure | `Smtp2GoValidationException` listing every problem. |
| Per-request timeout (`RequestOptions.Timeout` or `Smtp2GoClientOptions.Timeout`) | `TimeoutException` with the `OperationCanceledException` as inner. |
| Caller cancellation | `OperationCanceledException`, not reported to diagnostics. |
| Connection, DNS, TLS failures | `HttpRequestException`. |

## Handling

```csharp
try
{
    var response = await client.Raw.SendAsync<JsonElement, JsonElement>("webhook/add", body);
}
catch (Smtp2GoRateLimitException ex) when (ex.RetryAfter is { } wait)
{
    await Task.Delay(wait);
}
catch (Smtp2GoPermissionException ex)
{
    logger.LogError("Key lacks permission for {Endpoint} ({RequestId})", ex.Path, ex.RequestId);
}
catch (Smtp2GoApiException ex) when (ex.Code == Smtp2GoErrorCode.NonValidatingInPayload)
{
    foreach (var problem in ex.FieldValidationErrors)
    {
        logger.LogWarning("{Field}: {Message}", problem.FieldName, problem.Message);
    }
}
```

`Smtp2GoErrorCode` lists the codes the library knows (`ENDPOINT_PERMISSION_DENIED`, `NON_VALIDATING_IN_PAYLOAD`); the `E_ApiResponseCodes.` prefix and letter case are ignored when parsing, and the raw string is always on `ErrorCode`. The API key never appears in an exception, a diagnostic or an `Activity` tag.
