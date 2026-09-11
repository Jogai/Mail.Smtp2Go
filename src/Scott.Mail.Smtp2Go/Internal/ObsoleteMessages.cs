namespace Scott.Mail.Smtp2Go;

/// <summary>Messages for <c>[Obsolete]</c> on deprecated endpoints, kept on a non-obsolete type so attribute arguments do not trigger CS0618.</summary>
internal static class ObsoleteMessages
{
    public const string EmailSearch = "SMTP2GO has deprecated /email/search and will remove it in a future API version. Use client.Activity.SearchAsync.";
    public const string StatsRejects = "SMTP2GO has deprecated bounce_rejects and spam_rejects; use Rejects.";
}
