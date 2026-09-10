using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Scott.Mail.Smtp2Go.Json;

namespace Scott.Mail.Smtp2Go.Transport;

/// <summary>
/// The single HTTP round trip every call goes through: option merging, subaccount injection, client-side validation, header authentication,
/// regional URL resolution, per-request timeout, envelope parsing, error mapping, tracing and diagnostics.
/// </summary>
internal sealed class Smtp2GoConnection
{
    private const string ApiKeyHeaderName = "X-Smtp2go-Api-Key";
    private const string BearerScheme = "Bearer";
    private const string JsonMediaType = "application/json";
    private const string SubaccountIdField = "subaccount_id";
    private static readonly byte[] s_emptyObject = Encoding.UTF8.GetBytes("{}");

    private readonly HttpClient _http;
    private readonly Smtp2GoClientOptions _options;
    private readonly ISmtp2GoDiagnostics _diagnostics;
    private readonly JsonSerializerOptions _json;

    public Smtp2GoConnection(HttpClient http, Smtp2GoClientOptions options, ISmtp2GoDiagnostics? diagnostics)
    {
        _http = http;
        _options = options;
        _diagnostics = diagnostics ?? NullSmtp2GoDiagnostics.Instance;
        _json = CreateJsonOptions(options.AdditionalJsonTypeInfoResolver);
    }

    /// <summary>The serializer options in effect: the library context, optionally combined with <see cref="Smtp2GoClientOptions.AdditionalJsonTypeInfoResolver"/>.</summary>
    public JsonSerializerOptions JsonOptions => _json;

    /// <summary>Sends <paramref name="body"/> to <paramref name="endpoint"/> and parses the envelope.</summary>
    /// <exception cref="Smtp2GoValidationException">Client-side validation failed; nothing was sent.</exception>
    /// <exception cref="Smtp2GoApiException">The API answered with a non-success status.</exception>
    /// <exception cref="TimeoutException">The per-request timeout elapsed.</exception>
    public async Task<ApiResponse<TResponse>> SendAsync<TRequest, TResponse>(Endpoint endpoint, TRequest? body, RequestOptions? options, CancellationToken cancellationToken)
    {
        JsonTypeInfo<ApiResponse<TResponse>> responseInfo = GetTypeInfo<ApiResponse<TResponse>>();
        byte[] payload = PrepareBody(endpoint, body, options);

        return await ExecuteAsync(
            endpoint,
            payload,
            options,
            async (response, ct) =>
            {
                using Stream stream = await ReadStreamAsync(response.Content, ct).ConfigureAwait(false);
                ApiResponse<TResponse>? result;
                try
                {
                    result = await JsonSerializer.DeserializeAsync(stream, responseInfo, ct).ConfigureAwait(false);
                }
                catch (JsonException ex)
                {
                    throw new Smtp2GoApiException(
                        FormattableString.Invariant($"SMTP2GO API request to '{endpoint.Path}' returned status {(int)response.StatusCode} with a body that could not be parsed: {ex.Message}"),
                        (int)response.StatusCode,
                        endpoint.Path,
                        null,
                        ex);
                }

                if (result is null)
                {
                    throw new Smtp2GoApiException(
                        FormattableString.Invariant($"SMTP2GO API request to '{endpoint.Path}' returned status {(int)response.StatusCode} with an empty body."),
                        (int)response.StatusCode,
                        endpoint.Path,
                        null);
                }

                return (result, result.RequestId);
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends <paramref name="body"/> to <paramref name="endpoint"/> and returns the whole envelope as a <see cref="JsonDocument"/>. The caller owns the document.</summary>
    public Task<JsonDocument> SendRawAsync(Endpoint endpoint, JsonElement body, RequestOptions? options, CancellationToken cancellationToken)
    {
        byte[] payload = PrepareBody(endpoint, body, options);

        return ExecuteAsync(
            endpoint,
            payload,
            options,
            async (response, ct) =>
            {
                using Stream stream = await ReadStreamAsync(response.Content, ct).ConfigureAwait(false);
                JsonDocument document = await JsonDocument.ParseAsync(stream, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip }, ct).ConfigureAwait(false);
                string? requestId = document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("request_id", out JsonElement id)
                    && id.ValueKind == JsonValueKind.String
                    ? id.GetString()
                    : null;
                return (document, requestId);
            },
            cancellationToken);
    }

    /// <summary>Resolves the <see cref="JsonTypeInfo{T}"/> for <typeparamref name="T"/> from the library context or the additional resolver.</summary>
    /// <exception cref="NotSupportedException"><typeparamref name="T"/> is registered nowhere.</exception>
    public JsonTypeInfo<T> GetTypeInfo<T>()
    {
        if (_json.TryGetTypeInfo(typeof(T), out JsonTypeInfo? info) && info is JsonTypeInfo<T> typed)
        {
            return typed;
        }

        throw new NotSupportedException(
            $"'{typeof(T)}' is not registered in Smtp2GoJsonContext. Use JsonElement or JsonNode for raw calls, or register the type (and ApiResponse<T>) in a JsonSerializerContext supplied through Smtp2GoClientOptions.AdditionalJsonTypeInfoResolver.");
    }

    private static JsonSerializerOptions CreateJsonOptions(IJsonTypeInfoResolver? additional)
    {
        JsonSerializerOptions baseOptions = Smtp2GoJsonContext.Default.Options;
        if (additional is null)
        {
            return baseOptions;
        }

        return new JsonSerializerOptions(baseOptions)
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(Smtp2GoJsonContext.Default, additional),
        };
    }

    /// <summary>Validates, serialises and (where the endpoint allows) merges <c>subaccount_id</c> into the body. Throws before any network activity.</summary>
    private byte[] PrepareBody<TRequest>(Endpoint endpoint, TRequest? body, RequestOptions? options)
    {
        List<string> errors = [];
        bool validate = _options.ClientSideValidation;
        if (validate && body is IRequestValidator validator)
        {
            validator.Validate(endpoint, errors);
        }

        string? subaccountId = options?.SubaccountId ?? _options.DefaultSubaccountId;
        bool inject = !string.IsNullOrEmpty(subaccountId) && endpoint.AcceptsSubaccountId;
        if (!string.IsNullOrEmpty(subaccountId) && !endpoint.AcceptsSubaccountId)
        {
            _diagnostics.SubaccountIdIgnored(endpoint);
        }

        byte[] payload;
        if (body is null || body is JsonElement { ValueKind: JsonValueKind.Undefined })
        {
            payload = inject ? SerializeObject(new JsonObject { [SubaccountIdField] = subaccountId }) : s_emptyObject;
        }
        else
        {
            JsonTypeInfo<TRequest> requestInfo = GetTypeInfo<TRequest>();
            if (inject)
            {
                if (JsonSerializer.SerializeToNode(body, requestInfo) is not JsonObject obj)
                {
                    throw new Smtp2GoValidationException(
                        FormattableString.Invariant($"Request to '{endpoint.Path}' failed client-side validation."),
                        ["subaccount_id can only be merged into a JSON object body."]);
                }

                obj[SubaccountIdField] = subaccountId;
                payload = SerializeObject(obj);
            }
            else
            {
                payload = JsonSerializer.SerializeToUtf8Bytes(body, requestInfo);
            }
        }

        if (validate && payload.Length > endpoint.MaxBodyBytes)
        {
            errors.Add(FormattableString.Invariant($"Request body is {payload.Length} bytes; the limit for '{endpoint.Path}' is {endpoint.MaxBodyBytes} bytes."));
        }

        if (errors.Count > 0)
        {
            _diagnostics.ValidationFailed(endpoint, errors);
            throw new Smtp2GoValidationException(FormattableString.Invariant($"Request to '{endpoint.Path}' failed client-side validation."), errors);
        }

        return payload;
    }

    private byte[] SerializeObject(JsonObject obj)
    {
        return JsonSerializer.SerializeToUtf8Bytes(obj, GetTypeInfo<JsonObject>());
    }

    private async Task<TResult> ExecuteAsync<TResult>(
        Endpoint endpoint,
        byte[] payload,
        RequestOptions? options,
        Func<HttpResponseMessage, CancellationToken, Task<(TResult Result, string? RequestId)>> readSuccess,
        CancellationToken cancellationToken)
    {
        (Uri baseUrl, Region? region) = ResolveBaseUrl(options);
        string apiKey = ResolveApiKey(options);

        using HttpRequestMessage request = new(endpoint.Method, new Uri(baseUrl, endpoint.Path));
        request.Content = new ByteArrayContent(payload);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(JsonMediaType) { CharSet = "utf-8" };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonMediaType));
        ApplyAuthentication(request, apiKey);
        RequestOptionKeys.SetEndpoint(request, endpoint);
        if (options?.AllowRetry == true)
        {
            RequestOptionKeys.SetAllowRetry(request);
        }

        using Activity? activity = Smtp2GoActivitySource.Instance.StartActivity(endpoint.Path, ActivityKind.Client);
        activity?.SetTag(Smtp2GoActivitySource.EndpointTag, endpoint.Path);
        activity?.SetTag(Smtp2GoActivitySource.RegionTag, region?.ToString() ?? "custom");
        activity?.SetTag(Smtp2GoActivitySource.HttpMethodTag, endpoint.Method.Method);

        TimeSpan timeout = options?.Timeout ?? _options.Timeout;
        using CancellationTokenSource? timeoutSource = timeout == Timeout.InfiniteTimeSpan ? null : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource?.CancelAfter(timeout);
        CancellationToken ct = timeoutSource?.Token ?? cancellationToken;

        long started = Stopwatch.GetTimestamp();
        string? requestId = null;
        _diagnostics.RequestStarting(endpoint, region);
        try
        {
            using HttpResponseMessage response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            int status = (int)response.StatusCode;
            activity?.SetTag(Smtp2GoActivitySource.HttpStatusTag, status);

            if (!response.IsSuccessStatusCode)
            {
                string? rawBody = await ReadStringAsync(response.Content, ct).ConfigureAwait(false);
                ApiError error = ApiErrorParser.Parse(rawBody);
                requestId = error.RequestId;
                throw ApiExceptionFactory.Create(endpoint, response.StatusCode, response.ReasonPhrase, error, ApiExceptionFactory.GetRetryAfter(response.Headers));
            }

            (TResult result, requestId) = await readSuccess(response, ct).ConfigureAwait(false);
            activity?.SetTag(Smtp2GoActivitySource.RequestIdTag, requestId);
            activity?.SetStatus(ActivityStatusCode.Ok);
            _diagnostics.RequestCompleted(endpoint, status, requestId, Elapsed(started));
            return result;
        }
        catch (OperationCanceledException ex) when (timeoutSource is { IsCancellationRequested: true } && !cancellationToken.IsCancellationRequested)
        {
            TimeoutException timeoutException = new(FormattableString.Invariant($"SMTP2GO API request to '{endpoint.Path}' did not complete within {timeout}."), ex);
            ReportFailure(activity, endpoint, timeoutException, requestId);
            throw timeoutException;
        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "cancelled");
            throw;
        }
        catch (Exception ex)
        {
            ReportFailure(activity, endpoint, ex, requestId);
            throw;
        }
    }

    private void ReportFailure(Activity? activity, Endpoint endpoint, Exception exception, string? requestId)
    {
        activity?.SetTag(Smtp2GoActivitySource.RequestIdTag, requestId);
        activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        _diagnostics.RequestFailed(endpoint, exception, requestId);
    }

    private (Uri BaseUrl, Region? Region) ResolveBaseUrl(RequestOptions? options)
    {
        Region? region = options?.Region ?? _options.Region;
        if (region is { } resolved)
        {
            return (RegionEndpoints.GetBaseUrl(resolved), resolved);
        }

        if (_options.BaseUrl is { } custom)
        {
            return (EnsureTrailingSlash(custom), null);
        }

        return (RegionEndpoints.Global, Smtp2Go.Region.Global);
    }

    private string ResolveApiKey(RequestOptions? options)
    {
        if (options?.ApiKeyOverride is { } overrideKey)
        {
            if (string.IsNullOrWhiteSpace(overrideKey))
            {
                throw new ArgumentException("RequestOptions.ApiKeyOverride must not be empty.", nameof(options));
            }

            return overrideKey;
        }

        string? apiKey = _options.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new Smtp2GoValidationException("Smtp2GoClientOptions is not valid.", ["ApiKey is required."]);
        }

        return apiKey!;
    }

    private void ApplyAuthentication(HttpRequestMessage request, string apiKey)
    {
        if (_options.AuthenticationScheme == AuthenticationScheme.Bearer)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(BearerScheme, apiKey);
        }
        else
        {
            request.Headers.TryAddWithoutValidation(ApiKeyHeaderName, apiKey);
        }
    }

    private static Uri EnsureTrailingSlash(Uri baseUrl)
    {
        string text = baseUrl.AbsoluteUri;
        return text.Length > 0 && text[text.Length - 1] == '/' ? baseUrl : new Uri(text + "/");
    }

    private static TimeSpan Elapsed(long started)
    {
        return TimeSpan.FromSeconds((Stopwatch.GetTimestamp() - started) / (double)Stopwatch.Frequency);
    }

    private static Task<Stream> ReadStreamAsync(HttpContent content, CancellationToken cancellationToken)
    {
#if NET8_0_OR_GREATER
        return content.ReadAsStreamAsync(cancellationToken);
#else
        cancellationToken.ThrowIfCancellationRequested();
        return content.ReadAsStreamAsync();
#endif
    }

    private static Task<string> ReadStringAsync(HttpContent content, CancellationToken cancellationToken)
    {
#if NET8_0_OR_GREATER
        return content.ReadAsStringAsync(cancellationToken);
#else
        cancellationToken.ThrowIfCancellationRequested();
        return content.ReadAsStringAsync();
#endif
    }
}
