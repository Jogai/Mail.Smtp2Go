using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// The <c>data</c> of <c>/email/send</c> and <c>/email/mime</c>. The endpoint answers 200 even when every recipient failed, so check
/// <see cref="Failed"/> and <see cref="Failures"/>, or call <see cref="EnsureAccepted"/> to turn failures into <see cref="Smtp2GoSendException"/>.
/// With <c>fastaccept</c> the counts are absent and only <see cref="EmailId"/> is returned.
/// </summary>
public sealed record EmailSendResult
{
    /// <summary>Number of recipients the email was sent to. Absent with <c>fastaccept</c>.</summary>
    [JsonPropertyName("succeeded")]
    public int? Succeeded { get; init; }

    /// <summary>Number of recipients that failed. Absent with <c>fastaccept</c>.</summary>
    [JsonPropertyName("failed")]
    public int? Failed { get; init; }

    /// <summary>One message per failure. Absent with <c>fastaccept</c>.</summary>
    [JsonPropertyName("failures")]
    public IReadOnlyList<string>? Failures { get; init; }

    /// <summary>The id of the sent email, usable with the activity and archive endpoints. Absent when scheduled.</summary>
    [JsonPropertyName("email_id")]
    public string? EmailId { get; init; }

    /// <summary>The id of the queued email when <c>schedule</c> was given; usable with the scheduled search and remove endpoints.</summary>
    [JsonPropertyName("schedule_id")]
    public string? ScheduleId { get; init; }

    /// <summary>Any field this library does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; set; }

    /// <summary>Whether the email was queued for later delivery rather than sent.</summary>
    [JsonIgnore]
    public bool IsScheduled => !string.IsNullOrEmpty(ScheduleId);

    /// <summary>Whether the server reported at least one failure.</summary>
    [JsonIgnore]
    public bool HasFailures => Failed > 0 || Failures is { Count: > 0 };

    /// <summary>Returns this result, or throws <see cref="Smtp2GoSendException"/> listing <see cref="Failures"/> when <see cref="HasFailures"/> is true.</summary>
    /// <param name="requestId">The envelope's <c>request_id</c>, carried on the exception for support enquiries.</param>
    /// <exception cref="Smtp2GoSendException">The server reported failures.</exception>
    public EmailSendResult EnsureAccepted(string? requestId = null)
    {
        if (!HasFailures)
        {
            return this;
        }

        IReadOnlyList<string> failures = Failures ?? [];
        string message = FormattableString.Invariant($"SMTP2GO reported {Failed ?? failures.Count} failed and {Succeeded ?? 0} succeeded recipients")
            + (failures.Count > 0 ? ": " + string.Join("; ", failures) : ".")
            + (requestId is null ? string.Empty : FormattableString.Invariant($" (request_id {requestId})"));
        throw new Smtp2GoSendException(message, failures, requestId, this);
    }
}
