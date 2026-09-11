using System.Buffers;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Scott.Mail.Smtp2Go.Webhooks;

namespace Scott.Mail.Smtp2Go.AspNetCore;

/// <summary>
/// The request pipeline behind one <c>MapSmtp2GoWebhook</c> endpoint: media-type check (415), body limit (413), parse through the core
/// <see cref="WebhookPayloadParser"/> (400 on a malformed body), then the handler (200, or <see cref="Smtp2GoWebhookOptions.ReturnStatusOnHandlerError"/>).
/// The convention builder mutates the settings while the application starts; requests only read them.
/// </summary>
internal sealed class Smtp2GoWebhookEndpoint
{
    private const string JsonSubType = "json";
    private const string FormUrlEncoded = "application/x-www-form-urlencoded";
    private const string MultipartFormData = "multipart/form-data";

    private readonly Func<HttpContext, WebhookEvent, CancellationToken, Task> _handler;
    private readonly ILogger _logger;
    private readonly List<WebhookCredential> _credentials = [];
    private WebhookPayloadParser? _parser;

    public Smtp2GoWebhookEndpoint(Smtp2GoWebhookOptions options, Func<HttpContext, WebhookEvent, CancellationToken, Task> handler, ILoggerFactory loggerFactory)
    {
        Options = options;
        _handler = handler;
        _logger = loggerFactory.CreateLogger(Smtp2GoWebhookEventIds.CategoryName);
    }

    private enum PayloadFormat
    {
        Json,
        Form,
    }

    /// <summary>This endpoint's copy of the options; <see cref="Smtp2GoWebhookEndpointConventionBuilder.WithOptions"/> edits it in place.</summary>
    public Smtp2GoWebhookOptions Options { get; }

    /// <summary>Built on first use from <see cref="Smtp2GoWebhookOptions.KnownCustomHeaders"/>, so options edits made while mapping are honoured.</summary>
    private WebhookPayloadParser Parser
    {
        get
        {
            WebhookPayloadParser? parser = Volatile.Read(ref _parser);
            if (parser is null)
            {
                parser = new WebhookPayloadParser(new WebhookParserOptions { KnownCustomHeaders = [.. Options.KnownCustomHeaders] });
                Interlocked.CompareExchange(ref _parser, parser, null);
                parser = _parser;
            }

            return parser;
        }
    }

    /// <summary>Accepts one more <c>Authorization</c> value; the first call turns authentication on.</summary>
    public void AddCredential(WebhookCredential credential)
    {
        _credentials.Add(credential);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        CancellationToken cancellationToken = context.RequestAborted;
        string path = context.Request.Path.Value ?? "/";

        if (_credentials.Count > 0 && !WebhookAuthenticator.IsAuthorized(context.Request.Headers.Authorization, _credentials, context.RequestServices))
        {
            Smtp2GoWebhookLog.Unauthorized(_logger, path, PresentedScheme(context.Request.Headers.Authorization));
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            foreach (string challenge in _credentials.Select(credential => credential.Challenge).Distinct(StringComparer.Ordinal))
            {
                context.Response.Headers.Append(HeaderNames.WWWAuthenticate, challenge);
            }

            return;
        }

        if (!TryGetPayloadFormat(context.Request, out PayloadFormat format, out string mediaType))
        {
            Smtp2GoWebhookLog.UnsupportedMediaType(_logger, path, mediaType);
            context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
            return;
        }

        long maxBodyBytes = Options.MaxBodyBytes;
        if (context.Request.ContentLength > maxBodyBytes)
        {
            Smtp2GoWebhookLog.PayloadTooLarge(_logger, path, maxBodyBytes);
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }

        IHttpMaxRequestBodySizeFeature? sizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (sizeFeature is { IsReadOnly: false })
        {
            sizeFeature.MaxRequestBodySize = maxBodyBytes;
        }

        WebhookEvent webhookEvent;
        try
        {
            byte[]? body = await ReadBodyAsync(context.Request.Body, maxBodyBytes, cancellationToken).ConfigureAwait(false);
            if (body is null)
            {
                Smtp2GoWebhookLog.PayloadTooLarge(_logger, path, maxBodyBytes);
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                return;
            }

            webhookEvent = format == PayloadFormat.Json ? Parser.Parse(body) : await ParseFormAsync(context.Request, body, cancellationToken).ConfigureAwait(false);
        }
        catch (BadHttpRequestException exception) when (exception.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            Smtp2GoWebhookLog.PayloadTooLarge(_logger, path, maxBodyBytes);
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            Smtp2GoWebhookLog.PayloadInvalid(_logger, exception, path, mediaType);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        string kind = webhookEvent.Kind.ToString();
        Smtp2GoWebhookLog.CallbackReceived(_logger, kind, webhookEvent.EventRaw, webhookEvent.WebhookId, path, mediaType);
        long started = Stopwatch.GetTimestamp();
        try
        {
            await _handler(context, webhookEvent, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The client went away; there is nobody to answer.
            return;
        }
        catch (Exception exception)
        {
            int status = Options.ReturnStatusOnHandlerError;
            Smtp2GoWebhookLog.HandlerFailed(_logger, exception, kind, webhookEvent.WebhookId, status);
            context.Response.StatusCode = status;
            return;
        }

        double elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        Smtp2GoWebhookLog.CallbackHandled(_logger, kind, webhookEvent.WebhookId, elapsedMs);
        context.Response.StatusCode = StatusCodes.Status200OK;
    }

    /// <summary>The scheme word of the first Authorization value, for the log; never the credential itself.</summary>
    private static string PresentedScheme(StringValues authorization)
    {
        string? first = authorization.Count > 0 ? authorization[0] : null;
        if (string.IsNullOrWhiteSpace(first))
        {
            return "none";
        }

        string trimmed = first.Trim();
        int space = trimmed.IndexOf(' ', StringComparison.Ordinal);
        return space < 0 ? trimmed : trimmed.Substring(0, space);
    }

    private bool TryGetPayloadFormat(HttpRequest request, out PayloadFormat format, out string mediaType)
    {
        format = default;
        mediaType = request.ContentType ?? string.Empty;
        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out MediaTypeHeaderValue? parsed) || !parsed.MediaType.HasValue)
        {
            return false;
        }

        mediaType = parsed.MediaType.Value!;
        if (!Options.AllowedContentTypes.Any(allowed => string.Equals(allowed, parsed.MediaType.Value, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (StringSegment.Equals(parsed.SubType, JsonSubType, StringComparison.OrdinalIgnoreCase) || StringSegment.Equals(parsed.Suffix, JsonSubType, StringComparison.OrdinalIgnoreCase))
        {
            format = PayloadFormat.Json;
            return true;
        }

        if (StringSegment.Equals(parsed.MediaType, FormUrlEncoded, StringComparison.OrdinalIgnoreCase) || StringSegment.Equals(parsed.MediaType, MultipartFormData, StringComparison.OrdinalIgnoreCase))
        {
            format = PayloadFormat.Form;
            return true;
        }

        return false;
    }

    /// <summary>Reads the whole body, or returns <see langword="null"/> as soon as it exceeds <paramref name="maxBytes"/> (a chunked request has no Content-Length to check up front).</summary>
    private static async Task<byte[]?> ReadBodyAsync(Stream body, long maxBytes, CancellationToken cancellationToken)
    {
        using MemoryStream buffer = new();
        byte[] chunk = ArrayPool<byte>.Shared.Rent(16 * 1024);
        try
        {
            int read;
            while ((read = await body.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
            {
                if (buffer.Length + read > maxBytes)
                {
                    return null;
                }

                buffer.Write(chunk, 0, read);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(chunk);
        }

        return buffer.ToArray();
    }

    /// <summary>Lets ASP.NET Core's form reader split the buffered body (urlencoded or multipart) and feeds the pairs to the core parser.</summary>
    private async Task<WebhookEvent> ParseFormAsync(HttpRequest request, byte[] body, CancellationToken cancellationToken)
    {
        request.Body = new MemoryStream(body, writable: false);
        FormOptions formOptions = new()
        {
            BufferBody = false,
            MultipartBodyLengthLimit = Options.MaxBodyBytes,
            ValueLengthLimit = (int)Math.Min(Options.MaxBodyBytes, int.MaxValue),
        };
        IFormCollection form = await new FormFeature(request, formOptions).ReadFormAsync(cancellationToken).ConfigureAwait(false);
        return Parser.ParseForm(Flatten(form));
    }

    private static IEnumerable<KeyValuePair<string, string?>> Flatten(IFormCollection form)
    {
        foreach (KeyValuePair<string, StringValues> field in form)
        {
            foreach (string? value in field.Value)
            {
                yield return new KeyValuePair<string, string?>(field.Key, value);
            }
        }
    }
}
