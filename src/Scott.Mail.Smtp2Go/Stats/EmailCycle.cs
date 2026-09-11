using System.Text.Json.Serialization;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>stats/email_cycle</c>: the current billing cycle and its allowance.</summary>
[Smtp2GoEndpoint("stats/email_cycle")]
public sealed record EmailCycle
{
    /// <summary>Start of the cycle (UTC).</summary>
    [JsonPropertyName("cycle_start")]
    public DateTimeOffset? CycleStart { get; init; }

    /// <summary>End of the cycle (UTC).</summary>
    [JsonPropertyName("cycle_end")]
    public DateTimeOffset? CycleEnd { get; init; }

    /// <summary>Emails sent this cycle.</summary>
    [JsonPropertyName("cycle_used")]
    public long? CycleUsed { get; init; }

    /// <summary>Emails left this cycle.</summary>
    [JsonPropertyName("cycle_remaining")]
    public long? CycleRemaining { get; init; }

    /// <summary>The cycle allowance.</summary>
    [JsonPropertyName("cycle_max")]
    public long? CycleMax { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
