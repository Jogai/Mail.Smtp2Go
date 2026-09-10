namespace Scott.Mail.Smtp2Go.Transport;

/// <summary>The default <see cref="ISmtp2GoDiagnostics"/>: does nothing.</summary>
internal sealed class NullSmtp2GoDiagnostics : ISmtp2GoDiagnostics
{
    public static NullSmtp2GoDiagnostics Instance { get; } = new();

    private NullSmtp2GoDiagnostics()
    {
    }

    public void RequestStarting(Endpoint endpoint, Region? region)
    {
    }

    public void RequestCompleted(Endpoint endpoint, int statusCode, string? requestId, TimeSpan elapsed)
    {
    }

    public void RequestFailed(Endpoint endpoint, Exception exception, string? requestId)
    {
    }

    public void ValidationFailed(Endpoint endpoint, IReadOnlyList<string> errors)
    {
    }

    public void SubaccountIdIgnored(Endpoint endpoint)
    {
    }
}
