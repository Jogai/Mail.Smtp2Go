namespace Scott.Mail.Smtp2Go;

/// <summary>Client-side checks shared by the SMS reporting requests.</summary>
internal static class SmsRequestValidator
{
    public static void ValidateRange(DateTimeOffset? startDate, DateTimeOffset? endDate, ICollection<string> errors)
    {
        if (startDate is not null && endDate is not null && startDate > endDate)
        {
            errors.Add("start_date must not be after end_date.");
        }
    }
}
