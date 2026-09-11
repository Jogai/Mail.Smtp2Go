namespace Scott.Mail.Smtp2Go.Transport;

/// <summary>
/// Receives one notification per phase of an API call. The core package ships a no-op default and emits <see cref="System.Diagnostics.Activity"/>
/// spans through <see cref="Smtp2GoActivitySource"/>; the dependency injection package implements this with <c>ILogger</c>.
/// The API key never reaches this interface.
/// </summary>
public interface ISmtp2GoDiagnostics
{
    /// <summary>The request is about to be sent. <paramref name="region"/> is <see langword="null"/> when a custom base URL is in use.</summary>
    void RequestStarting(Endpoint endpoint, Region? region);

    /// <summary>The server answered with a success status and the body was parsed.</summary>
    void RequestCompleted(Endpoint endpoint, int statusCode, string? requestId, TimeSpan elapsed);

    /// <summary>The call failed: an API error was mapped to an exception, or the transport threw. Cancellation is not reported.</summary>
    void RequestFailed(Endpoint endpoint, Exception exception, string? requestId);

    /// <summary>Client-side validation rejected the request before any network activity.</summary>
    void ValidationFailed(Endpoint endpoint, IReadOnlyList<string> errors);

    /// <summary>A subaccount id was supplied for an endpoint that does not document <c>subaccount_id</c>; it was not sent.</summary>
    void SubaccountIdIgnored(Endpoint endpoint);

    /// <summary>A send call completed: <paramref name="succeeded"/> recipients were accepted and <paramref name="failed"/> were rejected by the server.</summary>
    void EmailResult(int succeeded, int failed);
}
