using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /sms/view-received</c>. The range includes <see cref="StartDate"/> and excludes <see cref="EndDate"/> (UTC). <c>subaccount_id</c> is injected from <see cref="RequestOptions.SubaccountId"/>.</summary>
[Smtp2GoEndpoint("sms/view-received")]
public sealed record SmsReceivedRequest : IRequestValidator
{
    /// <summary>Range start; server default: 7 days ago at midnight UTC.</summary>
    [JsonPropertyName("start_date")]
    public DateTimeOffset? StartDate { get; init; }

    /// <summary>Range end; server default: now.</summary>
    [JsonPropertyName("end_date")]
    public DateTimeOffset? EndDate { get; init; }

    /// <summary>The range start as a Unix timestamp. Deprecated by SMTP2GO in favour of <see cref="StartDate"/>.</summary>
    [JsonPropertyName("unix_start")]
    [Obsolete("Deprecated by SMTP2GO; use StartDate.")]
    public long? UnixStart { get; init; }

    /// <summary>The range end as a Unix timestamp. Deprecated by SMTP2GO in favour of <see cref="EndDate"/>.</summary>
    [JsonPropertyName("unix_end")]
    [Obsolete("Deprecated by SMTP2GO; use EndDate.")]
    public long? UnixEnd { get; init; }

    /// <summary>Only messages received through this username (SMTP user or API key).</summary>
    [JsonPropertyName("username")]
    public string? Username { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        SmsRequestValidator.ValidateRange(StartDate, EndDate, errors);
    }
}
