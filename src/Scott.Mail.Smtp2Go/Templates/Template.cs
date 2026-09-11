using System.Text.Json.Serialization;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// An email template. <c>template/add</c> and <c>template/edit</c> return the name as <c>template_name</c>; <c>template/search</c> and <c>template/view</c> return it as
/// <c>name</c>; <see cref="DisplayName"/> reads whichever is present. Bodies and variables are only returned by add, edit and view.
/// </summary>
[Smtp2GoEndpoint("template/view")]
public sealed record Template
{
    /// <summary>The case-sensitive template id (5 to 24 characters), used as <c>template_id</c> when sending.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>The name, as returned by <c>template/search</c> and <c>template/view</c>.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>The name, as returned by <c>template/add</c> and <c>template/edit</c>.</summary>
    [JsonPropertyName("template_name")]
    public string? TemplateName { get; init; }

    /// <summary><see cref="TemplateName"/> or <see cref="Name"/>, whichever the endpoint returned.</summary>
    [JsonIgnore]
    public string? DisplayName => TemplateName ?? Name;

    /// <summary>The subject, which may contain template variables.</summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; init; }

    /// <summary>The HTML body.</summary>
    [JsonPropertyName("html_body")]
    public string? HtmlBody { get; init; }

    /// <summary>The plain-text body.</summary>
    [JsonPropertyName("text_body")]
    public string? TextBody { get; init; }

    /// <summary>The template variables and their default values.</summary>
    [JsonPropertyName("template_variables")]
    public IReadOnlyDictionary<string, string>? TemplateVariables { get; init; }

    /// <summary>The tags.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>When the template was last changed (UTC).</summary>
    [JsonPropertyName("last_updated")]
    public DateTimeOffset? LastUpdated { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }
}
