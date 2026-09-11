namespace Scott.Mail.Smtp2Go;

/// <summary>The documented length limits of template names and ids.</summary>
internal static class TemplateRequestValidator
{
    public const int MaxNameLength = 64;
    public const int MinIdLength = 5;
    public const int MaxIdLength = 24;

    public static void ValidateName(string? name, bool required, ICollection<string> errors)
    {
        if (name is null)
        {
            if (required)
            {
                errors.Add("template_name is required.");
            }

            return;
        }

        if (name.Length < 1 || name.Length > MaxNameLength)
        {
            errors.Add($"template_name must be 1 to {MaxNameLength} characters.");
        }
    }

    public static void ValidateId(string? id, string field, bool required, ICollection<string> errors)
    {
        if (id is null)
        {
            if (required)
            {
                errors.Add($"{field} is required.");
            }

            return;
        }

        if (id.Length < MinIdLength || id.Length > MaxIdLength)
        {
            errors.Add($"{field} must be {MinIdLength} to {MaxIdLength} characters.");
        }
    }
}
