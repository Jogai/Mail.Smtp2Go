using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>POST /template/add</c>. The API requires every field except <see cref="TemplateVariables"/> and <see cref="Tags"/>.</summary>
[Smtp2GoEndpoint("template/add")]
public sealed record TemplateAddRequest : IRequestValidator
{
    /// <summary>The name, 1 to 64 characters.</summary>
    [JsonPropertyName("template_name")]
    public required string TemplateName { get; init; }

    /// <summary>The case-sensitive id, 5 to 24 characters.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>The subject.</summary>
    [JsonPropertyName("subject")]
    public required string Subject { get; init; }

    /// <summary>The HTML body.</summary>
    [JsonPropertyName("html_body")]
    public required string HtmlBody { get; init; }

    /// <summary>The plain-text body.</summary>
    [JsonPropertyName("text_body")]
    public required string TextBody { get; init; }

    /// <summary>The variables used in the template and their values, for example <c>{"variable1": "value1"}</c>.</summary>
    [JsonPropertyName("template_variables")]
    public IReadOnlyDictionary<string, string>? TemplateVariables { get; init; }

    /// <summary>Tags to associate with the template.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; init; }

    /// <inheritdoc />
    void IRequestValidator.Validate(Endpoint endpoint, ICollection<string> errors)
    {
        Argument.ThrowIfNull(errors);
        TemplateRequestValidator.ValidateName(TemplateName, required: true, errors);
        TemplateRequestValidator.ValidateId(Id, "id", required: true, errors);
        if (string.IsNullOrEmpty(Subject))
        {
            errors.Add("subject is required.");
        }

        if (HtmlBody is null)
        {
            errors.Add("html_body is required.");
        }

        if (TextBody is null)
        {
            errors.Add("text_body is required.");
        }
    }
}
