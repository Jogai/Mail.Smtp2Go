using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Shared;

/// <summary>Captures every <see cref="ISmtp2GoDiagnostics"/> notification for assertions.</summary>
public sealed class RecordingDiagnostics : ISmtp2GoDiagnostics
{
    public List<(Endpoint Endpoint, Region? Region)> Started { get; } = [];

    public List<(Endpoint Endpoint, int StatusCode, string? RequestId, TimeSpan Elapsed)> Completed { get; } = [];

    public List<(Endpoint Endpoint, Exception Exception, string? RequestId)> Failed { get; } = [];

    public List<(Endpoint Endpoint, IReadOnlyList<string> Errors)> ValidationFailures { get; } = [];

    public List<Endpoint> SubaccountIdIgnoredFor { get; } = [];

    public List<(int Succeeded, int Failed)> EmailResults { get; } = [];

    public void RequestStarting(Endpoint endpoint, Region? region)
    {
        Started.Add((endpoint, region));
    }

    public void RequestCompleted(Endpoint endpoint, int statusCode, string? requestId, TimeSpan elapsed)
    {
        Completed.Add((endpoint, statusCode, requestId, elapsed));
    }

    public void RequestFailed(Endpoint endpoint, Exception exception, string? requestId)
    {
        Failed.Add((endpoint, exception, requestId));
    }

    public void ValidationFailed(Endpoint endpoint, IReadOnlyList<string> errors)
    {
        ValidationFailures.Add((endpoint, errors));
    }

    public void SubaccountIdIgnored(Endpoint endpoint)
    {
        SubaccountIdIgnoredFor.Add(endpoint);
    }

    public void EmailResult(int succeeded, int failed)
    {
        EmailResults.Add((succeeded, failed));
    }
}
