using System.Globalization;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go.Webhooks;

/// <summary>
/// Resolves the <see cref="WebhookEventKind"/> from the <c>event</c> field and maps the normalised field bag onto the concrete subtype.
/// Shared by the JSON and form paths so both produce identical instances. Fields a kind does not consume go to <see cref="WebhookEvent.Extra"/>
/// unless they match a declared custom header.
/// </summary>
internal static class WebhookEventFactory
{
    private const string EventKey = "event";
    private const string TimeKey = "time";
    private const string IdKey = "id";

    private static readonly Dictionary<string, WebhookEventKind> s_kinds = new(StringComparer.Ordinal)
    {
        ["processed"] = WebhookEventKind.EmailProcessed,
        ["delivered"] = WebhookEventKind.EmailDelivered,
        ["bounce"] = WebhookEventKind.EmailBounce,
        ["bounced"] = WebhookEventKind.EmailBounce,
        ["open"] = WebhookEventKind.EmailOpen,
        ["opened"] = WebhookEventKind.EmailOpen,
        ["click"] = WebhookEventKind.EmailClick,
        ["clicked"] = WebhookEventKind.EmailClick,
        ["spam"] = WebhookEventKind.EmailSpam,
        ["spam_complaint"] = WebhookEventKind.EmailSpam,
        ["unsubscribe"] = WebhookEventKind.EmailUnsubscribe,
        ["unsubscribed"] = WebhookEventKind.EmailUnsubscribe,
        ["resubscribe"] = WebhookEventKind.EmailResubscribe,
        ["resubscribed"] = WebhookEventKind.EmailResubscribe,
        ["reject"] = WebhookEventKind.EmailReject,
        ["rejected"] = WebhookEventKind.EmailReject,
        ["sms_sending"] = WebhookEventKind.SmsSending,
        ["sms_submitted"] = WebhookEventKind.SmsSubmitted,
        ["sms_delivered"] = WebhookEventKind.SmsDelivered,
        ["sms_failed"] = WebhookEventKind.SmsFailed,
        ["sms_rejected"] = WebhookEventKind.SmsRejected,
        ["sms_opt_out"] = WebhookEventKind.SmsOptOut,
        ["sms_optout"] = WebhookEventKind.SmsOptOut,
    };

    private static readonly string[] s_commonKeys = [EventKey, TimeKey, IdKey];

    private static readonly string[] s_emailKeys =
    [
        "email_id", "message_id", "subject", "sender", "from", "from_address", "from_name", "rcpt", "recipients", "sendtime", "auth", "srchost", "host", "message", "context",
    ];

    private static readonly string[] s_bounceKeys = ["bounce"];
    private static readonly string[] s_openKeys = ["user_agent", "read_secs", "client", "client_device", "client_os", "geoip_continent", "geoip_country", "geoip_city"];
    private static readonly string[] s_clickKeys = ["link", "click_url"];

    private static readonly string[] s_smsKeys =
    [
        "destination_number", "email_subject", "message_content", "message_id", "received_timestamp", "region", "retry_count", "sender_email", "source_number", "status_code", "submitted_timestamp",
    ];

    public static WebhookEventKind ResolveKind(string? eventRaw)
    {
        if (string.IsNullOrWhiteSpace(eventRaw))
        {
            return WebhookEventKind.Unknown;
        }

        return s_kinds.TryGetValue(PayloadField.NormalizeKey(eventRaw!), out WebhookEventKind kind) ? kind : WebhookEventKind.Unknown;
    }

    public static WebhookEvent Create(IReadOnlyDictionary<string, PayloadField> fields, WebhookParserOptions options)
    {
        string eventRaw = Get(fields, EventKey) ?? string.Empty;
        WebhookEventKind kind = ResolveKind(eventRaw);
        HashSet<string> consumed = new(s_commonKeys, StringComparer.Ordinal);

        WebhookEvent result;
        switch (kind)
        {
            case WebhookEventKind.EmailProcessed:
                result = Fill(new EmailProcessedEvent { Kind = kind, EventRaw = eventRaw }, fields, consumed);
                break;
            case WebhookEventKind.EmailDelivered:
                result = Fill(new EmailDeliveredEvent { Kind = kind, EventRaw = eventRaw }, fields, consumed);
                break;
            case WebhookEventKind.EmailBounce:
                consumed.UnionWith(s_bounceKeys);
                string? bounceRaw = Get(fields, "bounce");
                result = Fill(new EmailBounceEvent { Kind = kind, EventRaw = eventRaw, BounceRaw = bounceRaw, BounceType = ParseBounce(bounceRaw) }, fields, consumed);
                break;
            case WebhookEventKind.EmailOpen:
                consumed.UnionWith(s_openKeys);
                result = Fill(FillOpen(new EmailOpenEvent { Kind = kind, EventRaw = eventRaw }, fields), fields, consumed);
                break;
            case WebhookEventKind.EmailClick:
                consumed.UnionWith(s_openKeys);
                consumed.UnionWith(s_clickKeys);
                result = Fill(FillOpen(new EmailClickEvent { Kind = kind, EventRaw = eventRaw, Link = Get(fields, "link"), ClickUrl = Get(fields, "click_url") }, fields), fields, consumed);
                break;
            case WebhookEventKind.EmailSpam:
                result = Fill(new EmailSpamEvent { Kind = kind, EventRaw = eventRaw }, fields, consumed);
                break;
            case WebhookEventKind.EmailUnsubscribe:
                result = Fill(new EmailUnsubscribeEvent { Kind = kind, EventRaw = eventRaw }, fields, consumed);
                break;
            case WebhookEventKind.EmailResubscribe:
                result = Fill(new EmailResubscribeEvent { Kind = kind, EventRaw = eventRaw }, fields, consumed);
                break;
            case WebhookEventKind.EmailReject:
                result = Fill(new EmailRejectEvent { Kind = kind, EventRaw = eventRaw }, fields, consumed);
                break;
            case WebhookEventKind.SmsSending:
            case WebhookEventKind.SmsSubmitted:
            case WebhookEventKind.SmsDelivered:
            case WebhookEventKind.SmsFailed:
            case WebhookEventKind.SmsRejected:
            case WebhookEventKind.SmsOptOut:
                consumed.UnionWith(s_smsKeys);
                DateTimeOffset? received = GetTime(fields, "received_timestamp");
                result = new SmsStatusEvent
                {
                    Kind = kind,
                    EventRaw = eventRaw,
                    WebhookId = Get(fields, IdKey),
                    Time = GetTime(fields, TimeKey) ?? received,
                    DestinationNumber = Get(fields, "destination_number"),
                    EmailSubject = Get(fields, "email_subject"),
                    MessageContent = Get(fields, "message_content"),
                    MessageId = Get(fields, "message_id"),
                    ReceivedTimestamp = received,
                    Region = Get(fields, "region"),
                    RetryCount = GetInt(fields, "retry_count"),
                    SenderEmail = Get(fields, "sender_email"),
                    SourceNumber = Get(fields, "source_number"),
                    StatusCode = Get(fields, "status_code"),
                    SubmittedTimestamp = GetTime(fields, "submitted_timestamp"),
                    Extra = CollectExtra(fields, consumed, null, out _),
                };
                return result;
            default:
                return new UnknownWebhookEvent
                {
                    Kind = WebhookEventKind.Unknown,
                    EventRaw = eventRaw,
                    WebhookId = Get(fields, IdKey),
                    Time = GetTime(fields, TimeKey),
                    Extra = CollectExtra(fields, consumed, null, out _),
                };
        }

        Dictionary<string, JsonElement>? extra = CollectExtra(fields, consumed, options.KnownCustomHeaders, out Dictionary<string, string>? customHeaders);
        return result switch
        {
            EmailClickEvent click => click with { Extra = extra, CustomHeaders = customHeaders },
            EmailOpenEvent open => open with { Extra = extra, CustomHeaders = customHeaders },
            EmailBounceEvent bounce => bounce with { Extra = extra, CustomHeaders = customHeaders },
            EmailProcessedEvent processed => processed with { Extra = extra, CustomHeaders = customHeaders },
            EmailDeliveredEvent delivered => delivered with { Extra = extra, CustomHeaders = customHeaders },
            EmailSpamEvent spam => spam with { Extra = extra, CustomHeaders = customHeaders },
            EmailUnsubscribeEvent unsubscribe => unsubscribe with { Extra = extra, CustomHeaders = customHeaders },
            EmailResubscribeEvent resubscribe => resubscribe with { Extra = extra, CustomHeaders = customHeaders },
            EmailRejectEvent reject => reject with { Extra = extra, CustomHeaders = customHeaders },
            _ => result,
        };
    }

    private static T Fill<T>(T target, IReadOnlyDictionary<string, PayloadField> fields, HashSet<string> consumed)
        where T : EmailWebhookEvent
    {
        consumed.UnionWith(s_emailKeys);
        return target with
        {
            WebhookId = Get(fields, IdKey),
            Time = GetTime(fields, TimeKey),
            EmailId = Get(fields, "email_id"),
            MessageId = Get(fields, "message_id"),
            Subject = Get(fields, "subject"),
            Sender = Get(fields, "sender"),
            From = Get(fields, "from"),
            FromAddress = Get(fields, "from_address"),
            FromName = Get(fields, "from_name"),
            Recipient = Get(fields, "rcpt"),
            Recipients = GetRecipients(fields),
            SendTime = GetTime(fields, "sendtime"),
            Auth = Get(fields, "auth"),
            SourceHost = Get(fields, "srchost"),
            Host = Get(fields, "host"),
            Message = Get(fields, "message"),
            Context = Get(fields, "context"),
        };
    }

    private static T FillOpen<T>(T target, IReadOnlyDictionary<string, PayloadField> fields)
        where T : EmailOpenEvent
    {
        return target with
        {
            UserAgent = Get(fields, "user_agent"),
            ReadSeconds = GetInt(fields, "read_secs"),
            Client = Get(fields, "client"),
            ClientDevice = Get(fields, "client_device"),
            ClientOs = Get(fields, "client_os"),
            GeoContinent = Get(fields, "geoip_continent"),
            GeoCountry = Get(fields, "geoip_country"),
            GeoCity = Get(fields, "geoip_city"),
        };
    }

    private static Dictionary<string, JsonElement>? CollectExtra(
        IReadOnlyDictionary<string, PayloadField> fields,
        HashSet<string> consumed,
        IReadOnlyCollection<string>? knownCustomHeaders,
        out Dictionary<string, string>? customHeaders)
    {
        customHeaders = null;
        Dictionary<string, JsonElement>? extra = null;
        Dictionary<string, string>? headerNames = null;
        if (knownCustomHeaders is { Count: > 0 })
        {
            headerNames = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string header in knownCustomHeaders)
            {
                if (!string.IsNullOrWhiteSpace(header))
                {
                    headerNames[PayloadField.NormalizeKey(header)] = header;
                }
            }
        }

        foreach (KeyValuePair<string, PayloadField> field in fields)
        {
            if (consumed.Contains(field.Key))
            {
                continue;
            }

            if (headerNames is not null && headerNames.TryGetValue(field.Key, out string? declaredName))
            {
                customHeaders ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                customHeaders[declaredName] = field.Value.First ?? string.Empty;
                continue;
            }

            extra ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            extra[field.Value.Name] = field.Value.Raw;
        }

        return extra;
    }

    private static string? Get(IReadOnlyDictionary<string, PayloadField> fields, string key)
    {
        return fields.TryGetValue(key, out PayloadField? field) ? field.First : null;
    }

    private static List<string>? GetRecipients(IReadOnlyDictionary<string, PayloadField> fields)
    {
        if (!fields.TryGetValue("recipients", out PayloadField? field))
        {
            return null;
        }

        List<string> recipients = [];
        foreach (string value in field.Values)
        {
            recipients.AddRange(FormPayloadReader.SplitRecipients(value));
        }

        return recipients;
    }

    private static DateTimeOffset? GetTime(IReadOnlyDictionary<string, PayloadField> fields, string key)
    {
        string? text = Get(fields, key);
        return text is not null && Smtp2GoDateTimeOffsetConverter.TryParse(text, out DateTimeOffset value) ? value : null;
    }

    private static int? GetInt(IReadOnlyDictionary<string, PayloadField> fields, string key)
    {
        string? text = Get(fields, key);
        return text is not null && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : null;
    }

    private static BounceType? ParseBounce(string? raw)
    {
        if (raw is null)
        {
            return null;
        }

        return PayloadField.NormalizeKey(raw) switch
        {
            "hard" => BounceType.Hard,
            "soft" => BounceType.Soft,
            _ => BounceType.Unknown,
        };
    }
}
