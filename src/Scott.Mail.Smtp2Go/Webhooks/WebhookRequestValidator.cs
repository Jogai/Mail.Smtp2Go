namespace Scott.Mail.Smtp2Go;

/// <summary>Client-side checks shared by <see cref="WebhookAddRequest"/> and <see cref="WebhookEditRequest"/>.</summary>
internal static class WebhookRequestValidator
{
    public static void Validate(
        string? url,
        IReadOnlyList<WebhookEmailEvent>? events,
        IReadOnlyList<WebhookSmsEvent>? smsEvents,
        WebhookOutputFormat? outputFormat,
        WebhookAuthHeaderType? authHeaderType,
        string? authHeaderValue,
        bool urlRequired,
        ICollection<string> errors)
    {
        if (url is null)
        {
            if (urlRequired)
            {
                errors.Add("url is required.");
            }
        }
        else if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add("url must be an absolute http or https URL.");
        }

        if (events is not null && events.Contains(WebhookEmailEvent.Unknown))
        {
            errors.Add("events must not contain WebhookEmailEvent.Unknown.");
        }

        if (smsEvents is not null && smsEvents.Contains(WebhookSmsEvent.Unknown))
        {
            errors.Add("sms_events must not contain WebhookSmsEvent.Unknown.");
        }

        if (outputFormat == WebhookOutputFormat.Unknown)
        {
            errors.Add("output_format must not be WebhookOutputFormat.Unknown.");
        }

        if (authHeaderType == WebhookAuthHeaderType.Unknown)
        {
            errors.Add("auth_header_type must not be WebhookAuthHeaderType.Unknown.");
        }
        else if (authHeaderType is WebhookAuthHeaderType.Basic or WebhookAuthHeaderType.Bearer && string.IsNullOrEmpty(authHeaderValue))
        {
            errors.Add("auth_header_value is required when auth_header_type is basic or bearer.");
        }
    }
}
