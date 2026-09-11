namespace Scott.Mail.Smtp2Go;

/// <summary>Client-side checks shared by the API key, SMTP user and authenticated IP requests, which carry the same sending settings.</summary>
internal static class CredentialRequestValidator
{
    public static void ValidateRequired(string? value, string field, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{field} is required.");
        }
    }

    /// <summary>The <c>status</c>, <c>custom_ratelimit_value</c> and <c>custom_ratelimit_period</c> rules.</summary>
    public static void ValidateSettings(CredentialStatus? status, int? customRateLimitValue, string? customRateLimitPeriod, ICollection<string> errors)
    {
        if (status == CredentialStatus.Unknown)
        {
            errors.Add("status must not be CredentialStatus.Unknown.");
        }

        if (customRateLimitValue is <= 0)
        {
            errors.Add("custom_ratelimit_value must be positive.");
        }

        if (customRateLimitPeriod is not null && string.IsNullOrWhiteSpace(customRateLimitPeriod))
        {
            errors.Add("custom_ratelimit_period must not be blank; use a period such as \"1 hour\", \"2 days\" or \"0:30:00\".");
        }
    }

    /// <summary>The <c>endpoints</c> list of an API key: every entry is a path or wildcard pattern, none is blank.</summary>
    public static void ValidateEndpoints(IReadOnlyList<string>? endpoints, ICollection<string> errors)
    {
        if (endpoints is null)
        {
            return;
        }

        for (int i = 0; i < endpoints.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(endpoints[i]))
            {
                errors.Add($"endpoints[{i}] must not be blank.");
            }
        }
    }
}
