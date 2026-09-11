using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>sms/summary</c>: totals for the period and a row per subaccount.</summary>
public sealed record SmsSummary
{
    /// <summary>Messages sent in the period.</summary>
    [JsonPropertyName("total_messages")]
    public long? TotalMessages { get; init; }

    /// <summary>SMS units used (a long message counts as several).</summary>
    [JsonPropertyName("total_units")]
    public long? TotalUnits { get; init; }

    /// <summary>The cost of the messages.</summary>
    [JsonPropertyName("total_cost")]
    public decimal? TotalCost { get; init; }

    /// <summary>The same figures per subaccount.</summary>
    [JsonPropertyName("subaccounts")]
    public IReadOnlyList<SmsSubaccountSummary>? Subaccounts { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
