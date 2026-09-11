using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The <c>data</c> of the deprecated <c>/email/search</c>: a count, the matching emails and a continuation token. Each email is kept as a raw
/// <see cref="JsonElement"/> because the endpoint is going away; the activity search gets the typed model.
/// </summary>
[Obsolete(ObsoleteMessages.EmailSearch)]
public sealed record EmailSearchResult
{
    /// <summary>Number of emails matched.</summary>
    [JsonPropertyName("count")]
    public int? Count { get; init; }

    /// <summary>The matching emails (<c>email_id</c>, <c>sender</c>, <c>recipient</c>, <c>status</c>, <c>smtpcode</c>, <c>opens</c>, ...).</summary>
    [JsonPropertyName("emails")]
    public IReadOnlyList<JsonElement>? Emails { get; init; }

    /// <summary>Token for the next page, or <see langword="null"/> at the end.</summary>
    [JsonPropertyName("continue_token")]
    public string? ContinueToken { get; init; }

    /// <summary>Any field this library does not model, including <c>status_counts</c> when requested.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
