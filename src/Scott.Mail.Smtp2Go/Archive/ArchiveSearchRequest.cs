using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The body of <c>POST /archive/search</c>. Every field is optional; the default range is today from midnight UTC to now, and sent mail takes around two minutes to
/// become searchable. Requires Email Archiving to be enabled for the account (a paid-plan feature).
/// </summary>
public sealed record ArchiveSearchRequest : IRequestValidator
{
    /// <summary>The maximum and server default <see cref="Limit"/>.</summary>
    public const int MaxLimit = 5000;

    /// <summary>Range start, inclusive (UTC). Server default: today at midnight.</summary>
    [JsonPropertyName("start_date")]
    public DateTimeOffset? StartDate { get; init; }

    /// <summary>Range end, exclusive (UTC). Server default: now.</summary>
    [JsonPropertyName("end_date")]
    public DateTimeOffset? EndDate { get; init; }

    /// <summary>Maximum number of emails to return, 1 to 5,000 (server default 5,000).</summary>
    [JsonPropertyName("limit")]
    public int? Limit { get; init; }

    /// <summary>Only emails sent by this SMTP user or API key.</summary>
    [JsonPropertyName("username")]
    public string? Username { get; init; }

    /// <summary>Only emails to this recipient.</summary>
    [JsonPropertyName("recipient")]
    public string? Recipient { get; init; }

    /// <summary>Only emails from this sender.</summary>
    [JsonPropertyName("sender")]
    public string? Sender { get; init; }

    /// <summary>Only emails with this envelope-from.</summary>
    [JsonPropertyName("envelope_from")]
    public string? EnvelopeFrom { get; init; }

    /// <summary>Only emails with this subject.</summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; init; }

    /// <summary>Only emails whose headers contain this substring.</summary>
    [JsonPropertyName("headers")]
    public string? Headers { get; init; }

    /// <summary>Continues a previous search that found more than <see cref="Limit"/> emails. <see cref="IArchiveClient.SearchAllAsync"/> manages it.</summary>
    [JsonPropertyName("continue_token")]
    public string? ContinueToken { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        if (Limit is { } limit && (limit < 1 || limit > MaxLimit))
        {
            errors.Add($"limit must be between 1 and {MaxLimit}.");
        }
    }
}
