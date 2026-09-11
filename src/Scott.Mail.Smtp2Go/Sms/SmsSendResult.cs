using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>sms/send</c>: how many messages went out, a count per status and (per the schema) one entry per message.</summary>
public sealed record SmsSendResult
{
    /// <summary>The number of messages per status, for example <c>{"queued": 1}</c>.</summary>
    [JsonPropertyName("statuses")]
    public IReadOnlyDictionary<string, int>? Statuses { get; init; }

    /// <summary>The number of messages sent.</summary>
    [JsonPropertyName("total_sent")]
    public int? TotalSent { get; init; }

    /// <summary>One entry per destination with its message id and status. The schema documents it; the example omits it.</summary>
    [JsonPropertyName("messages")]
    public IReadOnlyList<SmsSendMessage>? Messages { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
