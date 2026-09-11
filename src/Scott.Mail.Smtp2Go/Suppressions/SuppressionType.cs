using System.Text.Json.Serialization;
using Scott.Mail.Smtp2Go.Json.Converters;

namespace Scott.Mail.Smtp2Go;

/// <summary>Why an address is suppressed (the <c>reason</c> of a suppression, the <c>suppression_type(s)</c> filter, and the <c>reasons</c> to remove).</summary>
[JsonConverter(typeof(TolerantEnumConverter<SuppressionType>))]
public enum SuppressionType
{
    /// <summary>A value this library does not know; see <see cref="Suppression.ReasonRaw"/>. Never sent to the API.</summary>
    Unknown = 0,

    /// <summary>Added by hand or through <c>suppression/add</c>.</summary>
    Manual,

    /// <summary>The recipient complained about spam.</summary>
    Spam,

    /// <summary>The recipient unsubscribed.</summary>
    Unsubscribe,

    /// <summary>Delivery hard-bounced.</summary>
    Bounce,

    /// <summary>Suppressed by SMTP2GO compliance.</summary>
    Compliance,
}
