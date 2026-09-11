namespace Scott.Mail.Smtp2Go;

/// <summary>
/// Thrown by <see cref="EmailSendResult.EnsureAccepted"/> when a 200 response reports failed recipients. Not thrown by the client itself:
/// <c>/email/send</c> answers 200 with <c>failed</c> and <c>failures</c>, and this is the opt-in way to get exception semantics for that.
/// </summary>
public sealed class Smtp2GoSendException : Smtp2GoException
{
    /// <summary>Initializes a new instance.</summary>
    public Smtp2GoSendException()
    {
        Failures = [];
    }

    /// <summary>Initializes a new instance with a message.</summary>
    public Smtp2GoSendException(string message)
        : base(message)
    {
        Failures = [];
    }

    /// <summary>Initializes a new instance with a message and an inner exception.</summary>
    public Smtp2GoSendException(string message, Exception? innerException)
        : base(message, innerException)
    {
        Failures = [];
    }

    /// <summary>Initializes a new instance from a send result.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="failures">The server's failure messages.</param>
    /// <param name="requestId">The envelope's <c>request_id</c>, if known.</param>
    /// <param name="result">The full result.</param>
    public Smtp2GoSendException(string message, IReadOnlyList<string> failures, string? requestId, EmailSendResult? result)
        : base(message)
    {
        Failures = failures ?? [];
        RequestId = requestId;
        Result = result;
    }

    /// <summary>The server's failure messages, one per recipient that failed.</summary>
    public IReadOnlyList<string> Failures { get; }

    /// <summary>The server-assigned request id, when known.</summary>
    public string? RequestId { get; }

    /// <summary>The full result the failures came from.</summary>
    public EmailSendResult? Result { get; }
}
