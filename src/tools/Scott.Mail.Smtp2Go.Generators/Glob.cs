namespace Scott.Mail.Smtp2Go.Generators;

/// <summary>Wildcard matching for endpoint path patterns: <c>*</c> matches any run of characters (including <c>/</c>); everything else is literal and ordinal.</summary>
internal static class Glob
{
    public static bool IsMatch(string pattern, string text)
    {
        int p = 0;
        int t = 0;
        int starPattern = -1;
        int starText = -1;

        while (t < text.Length)
        {
            if (p < pattern.Length && pattern[p] == '*')
            {
                starPattern = p++;
                starText = t;
            }
            else if (p < pattern.Length && pattern[p] == text[t])
            {
                p++;
                t++;
            }
            else if (starPattern >= 0)
            {
                p = starPattern + 1;
                t = ++starText;
            }
            else
            {
                return false;
            }
        }

        while (p < pattern.Length && pattern[p] == '*')
        {
            p++;
        }

        return p == pattern.Length;
    }
}
