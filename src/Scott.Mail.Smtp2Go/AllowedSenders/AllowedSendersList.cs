using System.Text.Json;
using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of every <c>allowed_senders/*</c> endpoint: the list and how it is interpreted.</summary>
[Smtp2GoEndpoint("allowed_senders/view")]
public sealed record AllowedSendersList
{
    /// <summary>The addresses and domains on the list.</summary>
    [JsonPropertyName("allowed_senders")]
    public IReadOnlyList<string>? AllowedSenders { get; init; }

    /// <summary>How the list is interpreted; <see cref="AllowedSendersMode.Unknown"/> for a value this library does not know.</summary>
    [JsonPropertyName("mode")]
    public AllowedSendersMode? Mode { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
