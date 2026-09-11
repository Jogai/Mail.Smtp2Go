using System.Text.Json;
using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of every <c>allowed_recipients/*</c> endpoint: the list and whether it is applied when sending.</summary>
[Smtp2GoEndpoint("allowed_recipients/view")]
public sealed record AllowedRecipientsList
{
    /// <summary>The addresses and domains on the list.</summary>
    [JsonPropertyName("allowed_recipients")]
    public IReadOnlyList<string>? AllowedRecipients { get; init; }

    /// <summary>Whether the list is taken into account when sending.</summary>
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
