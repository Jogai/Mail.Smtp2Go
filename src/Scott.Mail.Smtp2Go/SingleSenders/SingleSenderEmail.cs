using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>One single sender address and its verification state.</summary>
public sealed record SingleSenderEmail
{
    /// <summary>The address.</summary>
    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; init; }

    /// <summary>Whether the address has been verified through the emailed link.</summary>
    [JsonPropertyName("verified")]
    public bool? Verified { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
