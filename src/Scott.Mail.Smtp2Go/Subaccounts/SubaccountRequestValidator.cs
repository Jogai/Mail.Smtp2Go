namespace Scott.Mail.Smtp2Go;

/// <summary>Client-side checks shared by the <c>subaccount/*</c> requests.</summary>
internal static class SubaccountRequestValidator
{
    public static void ValidateRequired(string? value, string field, ICollection<string> errors)
    {
        if (value is null || value.Trim().Length == 0)
        {
            errors.Add($"{field} is required.");
        }
    }

    public static void ValidateLimits(int? limit, int? smsLimit, ICollection<string> errors)
    {
        if (limit is <= 0)
        {
            errors.Add("limit must be positive.");
        }

        if (smsLimit is < 0)
        {
            errors.Add("sms_limit must not be negative.");
        }
    }
}
