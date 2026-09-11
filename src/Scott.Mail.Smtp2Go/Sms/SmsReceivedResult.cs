using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>sms/view-received</c>.</summary>
public sealed record SmsReceivedResult
{
    /// <summary>The messages received in the period.</summary>
    [JsonPropertyName("messages")]
    public IReadOnlyList<ReceivedSms>? Messages { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
