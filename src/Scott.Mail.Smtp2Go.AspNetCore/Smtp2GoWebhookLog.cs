using Microsoft.Extensions.Logging;

namespace Scott.Mail.Smtp2Go.AspNetCore;

/// <summary>The package's log messages. Never a body, a credential or a recipient address: only the callback kind, wire event name, delivery id and request facts.</summary>
internal static partial class Smtp2GoWebhookLog
{
    [LoggerMessage(EventId = Smtp2GoWebhookEventIds.CallbackReceived, Level = LogLevel.Debug, Message = "SMTP2GO callback {Kind} ({EventRaw}, id {WebhookId}) received at {Path} as {MediaType}")]
    public static partial void CallbackReceived(ILogger logger, string kind, string eventRaw, string? webhookId, string path, string mediaType);

    [LoggerMessage(EventId = Smtp2GoWebhookEventIds.CallbackHandled, Level = LogLevel.Information, Message = "SMTP2GO callback {Kind} (id {WebhookId}) handled in {ElapsedMs} ms")]
    public static partial void CallbackHandled(ILogger logger, string kind, string? webhookId, double elapsedMs);

    [LoggerMessage(EventId = Smtp2GoWebhookEventIds.UnsupportedMediaType, Level = LogLevel.Warning, Message = "SMTP2GO callback at {Path} rejected with 415: media type '{MediaType}' is not accepted")]
    public static partial void UnsupportedMediaType(ILogger logger, string path, string mediaType);

    [LoggerMessage(EventId = Smtp2GoWebhookEventIds.PayloadTooLarge, Level = LogLevel.Warning, Message = "SMTP2GO callback at {Path} rejected with 413: body exceeds {MaxBodyBytes} bytes")]
    public static partial void PayloadTooLarge(ILogger logger, string path, long maxBodyBytes);

    [LoggerMessage(EventId = Smtp2GoWebhookEventIds.PayloadInvalid, Level = LogLevel.Warning, Message = "SMTP2GO callback at {Path} rejected with 400: the {MediaType} body could not be parsed")]
    public static partial void PayloadInvalid(ILogger logger, Exception exception, string path, string mediaType);

    [LoggerMessage(EventId = Smtp2GoWebhookEventIds.HandlerFailed, Level = LogLevel.Error, Message = "SMTP2GO callback {Kind} (id {WebhookId}) handler failed; returning {StatusCode}")]
    public static partial void HandlerFailed(ILogger logger, Exception exception, string kind, string? webhookId, int statusCode);
}
