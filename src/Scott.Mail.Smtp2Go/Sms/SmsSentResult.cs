using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>sms/view-sent</c>.</summary>
public sealed record SmsSentResult
{
    /// <summary>The messages sent in the period.</summary>
    [JsonPropertyName("messages")]
    public IReadOnlyList<SentSms>? Messages { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
