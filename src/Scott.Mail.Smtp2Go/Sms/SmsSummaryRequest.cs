using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /sms/summary</c>. The range includes <see cref="StartDate"/> and excludes <see cref="EndDate"/> (UTC). <c>subaccount_id</c> is injected from <see cref="RequestOptions.SubaccountId"/>.</summary>
[Smtp2GoEndpoint("sms/summary")]
public sealed record SmsSummaryRequest : IRequestValidator
{
    /// <summary>Range start; server default: today at midnight UTC.</summary>
    [JsonPropertyName("start_date")]
    public DateTimeOffset? StartDate { get; init; }

    /// <summary>Range end; server default: now.</summary>
    [JsonPropertyName("end_date")]
    public DateTimeOffset? EndDate { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        SmsRequestValidator.ValidateRange(StartDate, EndDate, errors);
    }
}
