using Scott.Mail.Smtp2Go.AspNetCore;
using Scott.Mail.Smtp2Go.Webhooks;

namespace Scott.Mail.Smtp2Go.Demo.WebhookReceiver;

/// <summary>Catch-all: logs the identity of every callback. Registered for <see cref="WebhookEvent"/>, so it runs after any handler for the concrete type.</summary>
public sealed class LoggingHandler(ILogger<LoggingHandler> logger) : IWebhookEventHandler<WebhookEvent>
{
    public Task HandleAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        switch (webhookEvent)
        {
            case EmailWebhookEvent email:
                logger.LogInformation(
                    "{Kind} ({EventRaw}) at {Time}: email {EmailId} to {Recipient}, subject '{Subject}', customer {Customer}",
                    email.Kind,
                    email.EventRaw,
                    email.Time,
                    email.EmailId,
                    email.Recipient ?? string.Join(", ", email.Recipients ?? []),
                    email.Subject,
                    email.CustomHeaders?.GetValueOrDefault("X-Customer-Id") ?? "-");
                break;
            case SmsStatusEvent sms:
                logger.LogInformation("{Kind} at {Time}: SMS {MessageId} to {Destination}, status {Status}", sms.Kind, sms.Time, sms.MessageId, sms.DestinationNumber, sms.StatusCode);
                break;
            case UnknownWebhookEvent unknown:
                logger.LogWarning("Unknown event '{EventRaw}' with fields {Fields}", unknown.EventRaw, string.Join(", ", unknown.Extra?.Keys ?? []));
                break;
        }

        return Task.CompletedTask;
    }
}

/// <summary>Runs first for bounces (the concrete type wins): the place to suppress an address after a hard bounce.</summary>
public sealed class BounceHandler(ILogger<BounceHandler> logger) : IWebhookEventHandler<EmailBounceEvent>
{
    public Task HandleAsync(EmailBounceEvent bounce, CancellationToken cancellationToken)
    {
        if (bounce.BounceType == BounceType.Hard)
        {
            logger.LogWarning("Hard bounce for {Recipient} from {Host}: {Message}", bounce.Recipient, bounce.Host, bounce.Message ?? bounce.Context);
        }
        else
        {
            logger.LogInformation("Soft bounce for {Recipient}: {Message}", bounce.Recipient, bounce.Message ?? bounce.Context);
        }

        return Task.CompletedTask;
    }
}
