namespace Scott.Mail.Smtp2Go;

/// <summary>Client-side checks shared by the <c>domain/*</c> requests.</summary>
internal static class DomainRequestValidator
{
    private static readonly char[] s_invalidDomainCharacters = ['@', '/', ' '];

    public static void ValidateDomain(string? domain, ICollection<string> errors)
    {
        if (domain is null || domain.Trim().Length == 0)
        {
            errors.Add("domain is required.");
        }
        else if (domain.Trim().IndexOf('.') < 1 || domain.IndexOfAny(s_invalidDomainCharacters) != -1)
        {
            errors.Add("domain must be a bare domain name such as example.com.");
        }
    }

    public static void ValidateSubdomains(string? oldSubdomain, string? newSubdomain, ICollection<string> errors)
    {
        if (oldSubdomain is null || oldSubdomain.Trim().Length == 0)
        {
            errors.Add("old_subdomain is required.");
        }

        if (newSubdomain is null || newSubdomain.Trim().Length == 0)
        {
            errors.Add("new_subdomain is required.");
        }
    }

    public static void ValidateSubaccounts(IReadOnlyList<string>? subaccounts, ICollection<string> errors)
    {
        if (subaccounts is null)
        {
            return;
        }

        for (int i = 0; i < subaccounts.Count; i++)
        {
            if (subaccounts[i] is null || subaccounts[i].Trim().Length == 0)
            {
                errors.Add($"subaccounts[{i}] must not be blank.");
            }
        }
    }
}
