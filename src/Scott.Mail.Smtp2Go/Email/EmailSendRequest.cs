using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The body of <c>POST /email/send</c>. Only <see cref="Sender"/> and <see cref="To"/> are required by the API: <see cref="Subject"/> and the bodies are
/// ignored when <see cref="TemplateId"/> is set, and attachments may reference a URL instead of carrying a blob. Nulls are never serialised, so
/// the payload contains exactly the fields you set.
/// </summary>
public sealed record EmailSendRequest
{
    /// <summary>The sender, shown as <c>Name &lt;address&gt;</c>. Must be a verified sender domain or single sender on the account.</summary>
    [JsonPropertyName("sender")]
    public required EmailAddress Sender { get; init; }

    /// <summary>The recipients, up to 100.</summary>
    [JsonPropertyName("to")]
    public required IReadOnlyList<EmailAddress> To { get; init; }

    /// <summary>Carbon-copy recipients, up to 100.</summary>
    [JsonPropertyName("cc")]
    public IReadOnlyList<EmailAddress>? Cc { get; init; }

    /// <summary>Blind-carbon-copy recipients, up to 100.</summary>
    [JsonPropertyName("bcc")]
    public IReadOnlyList<EmailAddress>? Bcc { get; init; }

    /// <summary>The subject. Ignored when <see cref="TemplateId"/> is set; may contain <see cref="TemplateData"/> variables otherwise.</summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; init; }

    /// <summary>The HTML body. Required unless <see cref="TextBody"/> or <see cref="TemplateId"/> is set.</summary>
    [JsonPropertyName("html_body")]
    public string? HtmlBody { get; init; }

    /// <summary>The plain-text body. Required unless <see cref="HtmlBody"/> or <see cref="TemplateId"/> is set.</summary>
    [JsonPropertyName("text_body")]
    public string? TextBody { get; init; }

    /// <summary>Extra headers. <c>Content-Type</c>, <c>Content-Transfer-Encoding</c> and <c>MIME-Version</c> are rejected by the API.</summary>
    [JsonPropertyName("custom_headers")]
    public IReadOnlyList<CustomHeader>? CustomHeaders { get; init; }

    /// <summary>File attachments.</summary>
    [JsonPropertyName("attachments")]
    public IReadOnlyList<Attachment>? Attachments { get; init; }

    /// <summary>Inline images, referenced from <see cref="HtmlBody"/> as <c>cid:filename</c>.</summary>
    [JsonPropertyName("inlines")]
    public IReadOnlyList<Attachment>? Inlines { get; init; }

    /// <summary>The id of a template to render instead of <see cref="Subject"/>, <see cref="HtmlBody"/> and <see cref="TextBody"/>.</summary>
    [JsonPropertyName("template_id")]
    public string? TemplateId { get; init; }

    /// <summary>
    /// Variables for the template (and for <see cref="Subject"/> without a template). Values may be strings, numbers, booleans, string arrays,
    /// nested <c>Dictionary&lt;string, object?&gt;</c>, <see cref="System.Text.Json.JsonElement"/> or <see cref="System.Text.Json.Nodes.JsonNode"/>;
    /// other object types are not serialisable without reflection and are rejected in trimmed and AOT builds.
    /// </summary>
    [JsonPropertyName("template_data")]
    public IReadOnlyDictionary<string, object?>? TemplateData { get; init; }

    /// <summary>When to send: must be in the future and at most three days ahead. Sent as ISO-8601 UTC. The response then carries a <c>schedule_id</c>.</summary>
    [JsonPropertyName("schedule")]
    public DateTimeOffset? Schedule { get; init; }

    /// <summary>
    /// Accept the email immediately and send it in the background, so the response carries only <c>email_id</c> and no per-recipient outcome.
    /// Recommended by SMTP2GO and expected to become the default. <see langword="null"/> uses <see cref="Smtp2GoClientOptions.DefaultFastAccept"/>, then the server default.
    /// </summary>
    [JsonPropertyName("fastaccept")]
    public bool? FastAccept { get; init; }
}
