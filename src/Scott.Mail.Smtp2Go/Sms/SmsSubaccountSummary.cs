using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>One subaccount's row of <c>sms/summary</c>.</summary>
public sealed record SmsSubaccountSummary
{
    /// <summary>The subaccount id.</summary>
    [JsonPropertyName("subaccount_id")]
    public string? SubaccountId { get; init; }

    /// <summary>Messages sent by the subaccount in the period.</summary>
    [JsonPropertyName("total_messages")]
    public long? TotalMessages { get; init; }

    /// <summary>SMS units used by the subaccount.</summary>
    [JsonPropertyName("total_units")]
    public long? TotalUnits { get; init; }

    /// <summary>The cost of the subaccount's messages.</summary>
    [JsonPropertyName("total_cost")]
    public decimal? TotalCost { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
