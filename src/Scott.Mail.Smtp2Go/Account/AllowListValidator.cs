namespace Scott.Mail.Smtp2Go;

/// <summary>Client-side checks for the <c>allowed_senders</c> and <c>allowed_recipients</c> lists of addresses and domains.</summary>
internal static class AllowListValidator
{
    public static void ValidateEntries(IReadOnlyList<string>? entries, string field, ICollection<string> errors, bool allowEmpty = false)
    {
        if (entries is null)
        {
            errors.Add($"{field} is required.");
            return;
        }

        if (entries.Count == 0 && !allowEmpty)
        {
            errors.Add($"{field} must list at least one address or domain.");
        }

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] is null || entries[i].Trim().Length == 0)
            {
                errors.Add($"{field}[{i}] must not be blank.");
            }
        }
    }
}
