namespace Scott.Mail.Smtp2Go;

/// <summary>Thrown before any network activity when options or a request body fail client-side validation.</summary>
public sealed class Smtp2GoValidationException : Smtp2GoException
{
    /// <summary>Initializes a new instance.</summary>
    public Smtp2GoValidationException()
        : this("Validation failed.", [])
    {
    }

    /// <summary>Initializes a new instance with a message.</summary>
    public Smtp2GoValidationException(string message)
        : this(message, [])
    {
    }

    /// <summary>Initializes a new instance with a message and an inner exception.</summary>
    public Smtp2GoValidationException(string message, Exception? innerException)
        : base(message, innerException)
    {
        Errors = [];
    }

    /// <summary>Initializes a new instance listing every problem found.</summary>
    public Smtp2GoValidationException(string message, IReadOnlyList<string> errors)
        : base(BuildMessage(message, errors))
    {
        Errors = errors ?? [];
    }

    /// <summary>Every problem found, in the order detected.</summary>
    public IReadOnlyList<string> Errors { get; }

    private static string BuildMessage(string message, IReadOnlyList<string>? errors)
    {
        return errors is null || errors.Count == 0 ? message : message + " " + string.Join(" ", errors);
    }
}
