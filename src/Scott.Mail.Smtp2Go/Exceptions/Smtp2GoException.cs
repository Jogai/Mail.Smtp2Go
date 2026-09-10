namespace Scott.Mail.Smtp2Go;

/// <summary>Base type of every exception thrown by this library. Transport failures and cancellation are not wrapped and do not derive from it.</summary>
public class Smtp2GoException : Exception
{
    /// <summary>Initializes a new instance.</summary>
    public Smtp2GoException()
    {
    }

    /// <summary>Initializes a new instance with a message.</summary>
    public Smtp2GoException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and an inner exception.</summary>
    public Smtp2GoException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
