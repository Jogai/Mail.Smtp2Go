using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /template/edit</c>: the <see cref="Id"/> of the template plus the fields to change. Omitted fields are left as they are.</summary>
[Smtp2GoEndpoint("template/edit")]
public sealed record TemplateUpdateRequest : IRequestValidator
{
    /// <summary>The id of the template to change.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>A new id (5 to 24 characters).</summary>
    [JsonPropertyName("new_id")]
    public string? NewId { get; init; }

    /// <summary>A new name (1 to 64 characters).</summary>
    [JsonPropertyName("template_name")]
    public string? TemplateName { get; init; }

    /// <summary>A new subject.</summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; init; }

    /// <summary>A new HTML body.</summary>
    [JsonPropertyName("html_body")]
    public string? HtmlBody { get; init; }

    /// <summary>A new plain-text body.</summary>
    [JsonPropertyName("text_body")]
    public string? TextBody { get; init; }

    /// <summary>New template variables.</summary>
    [JsonPropertyName("template_variables")]
    public IReadOnlyDictionary<string, string>? TemplateVariables { get; init; }

    /// <summary>New tags.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        if (string.IsNullOrEmpty(Id))
        {
            errors.Add("id is required.");
        }

        TemplateRequestValidator.ValidateId(NewId, "new_id", required: false, errors);
        TemplateRequestValidator.ValidateName(TemplateName, required: false, errors);
    }
}
