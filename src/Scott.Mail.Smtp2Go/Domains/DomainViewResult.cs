using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The <c>data</c> of <c>domain/view</c>, <c>add</c>, <c>verify</c>, <c>remove</c>, <c>tracking</c> and <c>returnpath</c>: the sender domains concerned.</summary>
public sealed record DomainViewResult
{
    /// <summary>The domains; empty after removing the last one.</summary>
    [JsonPropertyName("domains")]
    public IReadOnlyList<SenderDomain>? Domains { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
