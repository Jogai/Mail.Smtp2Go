using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>How <c>activity/search</c> compares a subject, sender or recipient filter (<c>search_subject_op</c>, <c>search_sender_op</c>, <c>search_recipient_op</c>). Without one the server matches text the field contains.</summary>
[JsonConverter(typeof(TolerantEnumConverter<ActivitySearchOperator>))]
public enum ActivitySearchOperator
{
    /// <summary>A value this library does not know. Never sent to the API.</summary>
    Unknown = 0,

    /// <summary><c>equals</c>: the whole field matches.</summary>
    [JsonStringEnumMemberName("equals")]
    EqualTo,

    /// <summary><c>contains</c>: the field contains the text (the server's behaviour when no operator is passed).</summary>
    [JsonStringEnumMemberName("contains")]
    Contains,

    /// <summary><c>not_equals</c>: the whole field does not match.</summary>
    [JsonStringEnumMemberName("not_equals")]
    NotEqualTo,

    /// <summary><c>not_contains</c>: the field does not contain the text.</summary>
    [JsonStringEnumMemberName("not_contains")]
    DoesNotContain,
}
