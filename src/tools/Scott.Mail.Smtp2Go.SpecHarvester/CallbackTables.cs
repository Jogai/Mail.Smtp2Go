using System.Text.RegularExpressions;

namespace Scott.Mail.Smtp2Go.SpecHarvester;

/// <summary>Extracts the webhook callback parameter and event tables from the webhooks overview page.</summary>
public static partial class CallbackTables
{
    /// <summary>The pseudo-path for email callbacks.</summary>
    public const string EmailPath = "callbacks/email";

    /// <summary>The pseudo-path for SMS callbacks.</summary>
    public const string SmsPath = "callbacks/sms";

    /// <summary>Parses the <c>Webhook Events</c> and <c>Webhook Parameters</c> tables for email and SMS.</summary>
    public static IReadOnlyList<CallbackSummary> Parse(string overviewMarkdown)
    {
        ArgumentNullException.ThrowIfNull(overviewMarkdown);
        string text = overviewMarkdown.Replace("\r\n", "\n", StringComparison.Ordinal);
        return
        [
            Build(EmailPath, text, "Email"),
            Build(SmsPath, text, "SMS"),
        ];
    }

    private static CallbackSummary Build(string path, string text, string kind)
    {
        List<string[]> events = TableRows(Section(text, "## Webhook Events - " + kind));
        List<string[]> parameters = TableRows(Section(text, "## Webhook Parameters - " + kind));
        List<CallbackParameter> list = parameters
            .Where(r => r.Length > 0 && r[0].Length > 0)
            .Select(r => new CallbackParameter { Name = r[0], Description = r.Length > 1 ? r[1] : null })
            .ToList();
        string? eventDescription = list.FirstOrDefault(p => string.Equals(p.Name, "event", StringComparison.Ordinal))?.Description;
        return new CallbackSummary
        {
            Path = path,
            Events = events.Where(r => r.Length > 0 && r[0].Length > 0).Select(r => r[0]).ToList(),
            EventValues = eventDescription is null ? [] : QuotedValue().Matches(eventDescription).Select(m => m.Groups["value"].Value).ToList(),
            Parameters = list,
        };
    }

    /// <summary>The text from <paramref name="heading"/> to the next <c>## </c> heading.</summary>
    private static string Section(string text, string heading)
    {
        int start = text.IndexOf(heading + "\n", StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }

        start += heading.Length;
        int end = text.IndexOf("\n## ", start, StringComparison.Ordinal);
        return end < 0 ? text[start..] : text[start..end];
    }

    /// <summary>The body rows of the first Markdown table in <paramref name="section"/>, cells trimmed and unescaped.</summary>
    private static List<string[]> TableRows(string section)
    {
        List<string[]> rows = [];
        int seen = 0;
        foreach (string line in section.Split('\n'))
        {
            string trimmed = line.Trim();
            if (!trimmed.StartsWith('|'))
            {
                if (seen > 0)
                {
                    break;
                }

                continue;
            }

            seen++;
            if (seen <= 2)
            {
                continue; // header and alignment rows
            }

            string[] cells = trimmed.Trim('|').Split('|').Select(c => Unescape(c.Trim())).ToArray();
            rows.Add(cells);
        }

        return rows;
    }

    private static string Unescape(string cell)
    {
        return cell.Replace("\\_", "_", StringComparison.Ordinal).Replace("\\-", "-", StringComparison.Ordinal).Replace("\\*", "*", StringComparison.Ordinal);
    }

    [GeneratedRegex("\"(?<value>[a-z][a-z0-9_-]*)\"")]
    private static partial Regex QuotedValue();
}
