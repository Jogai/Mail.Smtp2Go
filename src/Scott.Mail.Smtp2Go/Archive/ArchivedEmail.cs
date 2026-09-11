using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>One archived email, as returned by <c>archive/search</c> (in <c>emails[]</c>) and <c>archive/email</c>.</summary>
[Smtp2GoEndpoint("archive/email")]
public sealed record ArchivedEmail
{
    /// <summary>The email id; pass it to <see cref="IArchiveClient.GetAsync"/>.</summary>
    [JsonPropertyName("email_id")]
    public string? EmailId { get; init; }

    /// <summary>The From header address.</summary>
    [JsonPropertyName("sender")]
    public string? Sender { get; init; }

    /// <summary>The envelope-from address.</summary>
    [JsonPropertyName("envelope_from")]
    public string? EnvelopeFrom { get; init; }

    /// <summary>The envelope recipient.</summary>
    [JsonPropertyName("recipient")]
    public string? Recipient { get; init; }

    /// <summary>The To header.</summary>
    [JsonPropertyName("to")]
    public string? To { get; init; }

    /// <summary>The subject.</summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; init; }

    /// <summary>When the email was sent (UTC).</summary>
    [JsonPropertyName("sent")]
    public DateTimeOffset? Sent { get; init; }

    /// <summary>The SMTP user or API key that sent it.</summary>
    [JsonPropertyName("username")]
    public string? Username { get; init; }

    /// <summary>The size of the original in bytes.</summary>
    [JsonPropertyName("byte_count")]
    public long? ByteCount { get; init; }

    /// <summary>The number of attachments.</summary>
    [JsonPropertyName("attachment_count")]
    public long? AttachmentCount { get; init; }

    /// <summary>The attachments; the docs do not describe the item shape, so each is kept raw.</summary>
    [JsonPropertyName("attachments")]
    public IReadOnlyList<JsonElement>? Attachments { get; init; }

    /// <summary>The raw headers of the email.</summary>
    [JsonPropertyName("headers")]
    public string? Headers { get; init; }

    /// <summary>A URL that downloads the original email; use <see cref="IArchiveClient.DownloadOriginalAsync"/>.</summary>
    [JsonPropertyName("url")]
    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "Wire field; the API may return a placeholder or a relative value.")]
    public string? Url { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
