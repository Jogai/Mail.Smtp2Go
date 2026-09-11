namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The documented limits of the <c>email/*</c> endpoints, checked before serialisation when <see cref="Smtp2GoClientOptions.ClientSideValidation"/> is on.
/// Every problem is reported; nothing throws here. The request models call in through <see cref="Transport.IRequestValidator"/>.
/// </summary>
internal static class EmailRequestValidator
{
    /// <summary>Recipients per <c>to</c>, <c>cc</c> and <c>bcc</c>.</summary>
    public const int MaxRecipientsPerField = 100;

    /// <summary>Maximum <c>limit</c> for <c>email/search</c>.</summary>
    public const int MaxSearchLimit = 5000;

    /// <summary>How far ahead <c>schedule</c> may point.</summary>
    public static readonly TimeSpan MaxScheduleAhead = TimeSpan.FromDays(3);

    private static readonly string[] s_disallowedHeaders = ["Content-Type", "Content-Transfer-Encoding", "MIME-Version"];

    public static void ValidateSend(EmailSendRequest request, ICollection<string> errors, string prefix = "")
    {
        if (request.Sender.IsEmpty)
        {
            errors.Add(prefix + "sender is required.");
        }

        ValidateRecipients(request.To, "to", required: true, errors, prefix);
        ValidateRecipients(request.Cc, "cc", required: false, errors, prefix);
        ValidateRecipients(request.Bcc, "bcc", required: false, errors, prefix);

        if (string.IsNullOrWhiteSpace(request.TextBody) && string.IsNullOrWhiteSpace(request.HtmlBody) && string.IsNullOrWhiteSpace(request.TemplateId))
        {
            errors.Add(prefix + "one of text_body, html_body or template_id is required.");
        }

        if (request.CustomHeaders is { } headers)
        {
            for (int i = 0; i < headers.Count; i++)
            {
                string field = FormattableString.Invariant($"{prefix}custom_headers[{i}]");
                CustomHeader header = headers[i];
                if (header is null || string.IsNullOrWhiteSpace(header.Header))
                {
                    errors.Add(field + " must have a header name.");
                }
                else if (Array.FindIndex(s_disallowedHeaders, h => string.Equals(h, header.Header.Trim(), StringComparison.OrdinalIgnoreCase)) >= 0)
                {
                    errors.Add(FormattableString.Invariant($"{field} '{header.Header}' is not allowed; the API rejects {string.Join(", ", s_disallowedHeaders)}."));
                }
            }
        }

        ValidateAttachments(request.Attachments, "attachments", errors, prefix);
        ValidateAttachments(request.Inlines, "inlines", errors, prefix);
        ValidateSchedule(request.Schedule, errors, prefix);
    }

    public static void ValidateMime(EmailMimeRequest request, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(request.MimeEmail))
        {
            errors.Add("mime_email is required.");
        }
        else if (!IsBase64(request.MimeEmail))
        {
            errors.Add("mime_email must be Base64-encoded.");
        }

        ValidateSchedule(request.Schedule, errors, string.Empty);
    }

    public static void ValidateBatch(EmailBatchRequest request, ICollection<string> errors)
    {
        IReadOnlyList<EmailSendRequest>? emails = request.Emails;
        if (emails is null || emails.Count == 0)
        {
            errors.Add("emails must contain at least one email.");
            return;
        }

        if (emails.Count > EmailBatchRequest.MaxEmails)
        {
            errors.Add(FormattableString.Invariant($"emails has {emails.Count} entries; the limit is {EmailBatchRequest.MaxEmails}."));
        }

        for (int i = 0; i < emails.Count; i++)
        {
            string prefix = FormattableString.Invariant($"emails[{i}].");
            if (emails[i] is null)
            {
                errors.Add(prefix.TrimEnd('.') + " is null.");
            }
            else
            {
                ValidateSend(emails[i], errors, prefix);
            }
        }
    }

    public static void ValidateScheduledSearch(ScheduledEmailSearchRequest request, ICollection<string> errors)
    {
        if (request.Limit is <= 0)
        {
            errors.Add("limit must be positive.");
        }

        if (request.Page is <= 0)
        {
            errors.Add("page must be 1 or greater.");
        }
    }

#pragma warning disable CS0618 // Validates the deprecated endpoint's documented limit.
    public static void ValidateSearch(EmailSearchRequest request, ICollection<string> errors)
#pragma warning restore CS0618
    {
        if (request.Limit is < 1 or > MaxSearchLimit)
        {
            errors.Add(FormattableString.Invariant($"limit must be between 1 and {MaxSearchLimit}."));
        }
    }

    /// <summary>Cheap Base64 check: length a multiple of four (ignoring whitespace), the standard alphabet, and at most two trailing <c>=</c>.</summary>
    public static bool IsBase64(string value)
    {
        int length = 0;
        int padding = 0;
        foreach (char c in value)
        {
            if (char.IsWhiteSpace(c))
            {
                continue;
            }

            length++;
            if (c == '=')
            {
                padding++;
                if (padding > 2)
                {
                    return false;
                }

                continue;
            }

            if (padding > 0)
            {
                return false;
            }

            bool inAlphabet = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '+' || c == '/';
            if (!inAlphabet)
            {
                return false;
            }
        }

        return length > 0 && length % 4 == 0;
    }

    private static void ValidateRecipients(IReadOnlyList<EmailAddress>? recipients, string field, bool required, ICollection<string> errors, string prefix)
    {
        if (recipients is null || recipients.Count == 0)
        {
            if (required)
            {
                errors.Add(prefix + field + " must contain at least one recipient.");
            }

            return;
        }

        if (recipients.Count > MaxRecipientsPerField)
        {
            errors.Add(FormattableString.Invariant($"{prefix}{field} has {recipients.Count} recipients; the limit is {MaxRecipientsPerField}."));
        }

        for (int i = 0; i < recipients.Count; i++)
        {
            if (recipients[i].IsEmpty)
            {
                errors.Add(FormattableString.Invariant($"{prefix}{field}[{i}] is empty."));
            }
        }
    }

    private static void ValidateAttachments(IReadOnlyList<Attachment>? attachments, string field, ICollection<string> errors, string prefix)
    {
        if (attachments is null)
        {
            return;
        }

        for (int i = 0; i < attachments.Count; i++)
        {
            string item = FormattableString.Invariant($"{prefix}{field}[{i}]");
            Attachment attachment = attachments[i];
            if (attachment is null)
            {
                errors.Add(item + " is null.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(attachment.Filename))
            {
                errors.Add(item + " must have a filename.");
            }

            bool hasBlob = !string.IsNullOrEmpty(attachment.Fileblob);
            bool hasUrl = !string.IsNullOrEmpty(attachment.Url);
            if (hasBlob == hasUrl)
            {
                errors.Add(item + " must set exactly one of fileblob or url.");
            }

            if (hasBlob && !IsBase64(attachment.Fileblob!))
            {
                errors.Add(item + ".fileblob must be Base64-encoded.");
            }
        }
    }

    private static void ValidateSchedule(DateTimeOffset? schedule, ICollection<string> errors, string prefix)
    {
        if (schedule is not { } when)
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (when <= now)
        {
            errors.Add(prefix + "schedule must be in the future.");
        }
        else if (when > now + MaxScheduleAhead)
        {
            errors.Add(FormattableString.Invariant($"{prefix}schedule must be at most {MaxScheduleAhead.TotalDays:0} days ahead."));
        }
    }
}
