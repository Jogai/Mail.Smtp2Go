using Microsoft.Extensions.Logging;

namespace Scott.Mail.Smtp2Go.AspNetCore;

/// <summary>The package's log messages. Never a body, a credential or a recipient address: only the callback kind, wire event name, delivery id and request facts.</summary>
internal static partial class Smtp2GoWebhookLog
{
    [LoggerMessage(EventId = Smtp2GoWebhookEventIds.CallbackReceived, Level = LogLevel.Debug, Message = "SMTP2GO callback {Kind} ({EventRaw}, id {WebhookId}) received at {Path} as {MediaType}")]
    public static partial void CallbackReceived(ILogger logger, string kind, string eventRaw, string? webhookId, string path, string mediaType);

    [LoggerMessage(EventId = Smtp2GoWebhookEventIds.CallbackHandled, Level = LogLevel.Information, Message = "SMTP2GO callback {Kind} (id {WebhookId}) handled in {ElapsedMs} ms")]
    public static partial void CallbackHandled(ILogger logger, string kind, string? webhookId, double elapsedMs);

    [LoggerMessage(EventId = Smtp2GoWebhookEventIds.NoHandlerRegistered, Level = LogLevel.Debug, Message = "SMTP2GO callback {Kind} ({EventType}) matched no registered IWebhookEventHandler")]
    public static partial void NoHandlerRegistered(ILogger logger, string kind, string eventType);

    [LoggerMessage(EventId = Smtp2GoWebhookEventIds.Unauthorized, Level = LogLevel.Warning, Message = "SMTP2GO callback at {Path} rejected with 401: no accepted credentials (presented scheme: {Scheme})")]
    public static partial void Unauthorized(ILogger logger, string path, string scheme);

    [LoggerMessage(EventId = Smtp2GoWebhookEventIds.SourceIpRejected, Level = LogLevel.Warning, Message = "SMTP2GO callback at {Path} rejected with 403: {RemoteIp} is not an address of {HostName}")]
    public static partial void SourceIpRejected(ILogger logger, string path, string remoteIp, string hostName);

    [LoggerMessage(EventId = Smtp2GoWebhookEventIds.SourceIpResolutionFailed, Level = LogLevel.Error, Message = "SMTP2GO callback at {Path} answered with 503: the addresses of {HostName} could not be resolved")]
    public static partial void SourceIpResolutionFailed(ILogger logger, Exception exception, string path, string hostName);

    [LoggerMessage(EventId = Smtp2GoWebhookEventIds.SourceIpResolved, Level = LogLevel.Debug, Message = "Resolved {HostName} to {Count} address(es); cached until {Expires}")]
    public static partial void SourceIpResolved(ILogger logger, string hostName, int count, DateTimeOffset expires);

    [LoggerMessage(EventId = Smtp2GoWebhookEventIds.SourceIpStaleCacheUsed, Level = LogLevel.Warning, Message = "Resolving {HostName} failed; serving the previous {Count} address(es) until the next attempt")]
    public static partial void SourceIpStaleCacheUsed(ILogger logger, Exception exception, string hostName, int count);

    [LoggerMessage(EventId = Smtp2GoWebhookEventIds.UnsupportedMediaType, Level = LogLevel.Warning, Message = "SMTP2GO callback at {Path} rejected with 415: media type '{MediaType}' is not accepted")]
    public static partial void UnsupportedMediaType(ILogger logger, string path, string mediaType);

    [LoggerMessage(EventId = Smtp2GoWebhookEventIds.PayloadTooLarge, Level = LogLevel.Warning, Message = "SMTP2GO callback at {Path} rejected with 413: body exceeds {MaxBodyBytes} bytes")]
    public static partial void PayloadTooLarge(ILogger logger, string path, long maxBodyBytes);

    [LoggerMessage(EventId = Smtp2GoWebhookEventIds.PayloadInvalid, Level = LogLevel.Warning, Message = "SMTP2GO callback at {Path} rejected with 400: the {MediaType} body could not be parsed")]
    public static partial void PayloadInvalid(ILogger logger, Exception exception, string path, string mediaType);

    [LoggerMessage(EventId = Smtp2GoWebhookEventIds.HandlerFailed, Level = LogLevel.Error, Message = "SMTP2GO callback {Kind} (id {WebhookId}) handler failed; returning {StatusCode}")]
    public static partial void HandlerFailed(ILogger logger, Exception exception, string kind, string? webhookId, int statusCode);
}
