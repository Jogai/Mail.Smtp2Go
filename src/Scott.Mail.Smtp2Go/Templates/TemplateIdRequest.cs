using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>template/view</c> and <c>template/delete</c>.</summary>
internal sealed record TemplateIdRequest
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
}
