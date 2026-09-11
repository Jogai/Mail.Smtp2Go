using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>single_sender_emails/view</c>.</summary>
public sealed record SingleSenderViewResult
{
    /// <summary>The addresses matching the request.</summary>
    [JsonPropertyName("senders")]
    public IReadOnlyList<SingleSenderEmail>? Senders { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
