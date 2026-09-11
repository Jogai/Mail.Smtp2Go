namespace Scott.Mail.Smtp2Go;

/// <summary>The documented username length of <c>users/smtp/add</c> (5 to 100 characters).</summary>
internal static class SmtpUserRequestValidator
{
    public const int MinUsernameLength = 5;
    public const int MaxUsernameLength = 100;

    public static void ValidateUsername(string? username, bool checkLength, ICollection<string> errors)
    {
        if (username is null || username.Trim().Length == 0)
        {
            errors.Add("username is required.");
            return;
        }

        if (checkLength && (username.Length < MinUsernameLength || username.Length > MaxUsernameLength))
        {
            errors.Add($"username must be {MinUsernameLength} to {MaxUsernameLength} characters.");
        }
    }
}
