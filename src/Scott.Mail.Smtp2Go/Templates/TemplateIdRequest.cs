using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>template/view</c> and <c>template/delete</c>.</summary>
[Smtp2GoEndpoint("template/delete")]
internal sealed record TemplateIdRequest
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
}
