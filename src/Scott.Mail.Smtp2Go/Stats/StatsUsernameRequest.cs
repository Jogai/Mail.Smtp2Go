using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>The body of <c>stats/email_summary</c>, <c>email_bounces</c>, <c>email_spam</c> and <c>email_unsubs</c>: the optional <c>username</c> filter and nothing else (these endpoints document no date range).</summary>
internal sealed record StatsUsernameRequest
{
    [JsonPropertyName("username")]
    public string? Username { get; init; }
}
