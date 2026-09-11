namespace Scott.Mail.Smtp2Go;

/// <summary>One-line send helpers over <see cref="IEmailClient"/> for the common cases.</summary>
public static class EmailClientExtensions
{
    /// <summary>Sends a single email with an HTML and/or plain-text body.</summary>
    /// <param name="client">The email client.</param>
    /// <param name="sender">The sender; a <see cref="string"/> such as <c>"Alice &lt;alice@example.com&gt;"</c> converts implicitly.</param>
    /// <param name="to">The recipient.</param>
    /// <param name="subject">The subject.</param>
    /// <param name="htmlBody">The HTML body, or <see langword="null"/> for text only.</param>
    /// <param name="textBody">The plain-text body, or <see langword="null"/> for HTML only.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    public static Task<ApiResponse<EmailSendResult>> SendAsync(
        this IEmailClient client,
        EmailAddress sender,
        EmailAddress to,
        string subject,
        string? htmlBody,
        string? textBody = null,
        CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(client);
        EmailSendRequest request = new()
        {
            Sender = sender,
            To = [to],
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody,
        };
        return client.SendAsync(request, null, cancellationToken);
    }

    /// <summary>Sends a single email rendered from a template.</summary>
    /// <param name="client">The email client.</param>
    /// <param name="sender">The sender.</param>
    /// <param name="to">The recipient.</param>
    /// <param name="templateId">The template id.</param>
    /// <param name="templateData">The template variables, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    public static Task<ApiResponse<EmailSendResult>> SendTemplateAsync(
        this IEmailClient client,
        EmailAddress sender,
        EmailAddress to,
        string templateId,
        IReadOnlyDictionary<string, object?>? templateData = null,
        CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(client);
        Argument.ThrowIfNullOrWhiteSpace(templateId);
        EmailSendRequest request = new()
        {
            Sender = sender,
            To = [to],
            TemplateId = templateId,
            TemplateData = templateData,
        };
        return client.SendAsync(request, null, cancellationToken);
    }
}
