using Microsoft.Extensions.Logging;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.DependencyInjection;

/// <summary>
/// <see cref="ISmtp2GoDiagnostics"/> over <see cref="ILogger"/> (category <see cref="CategoryName"/>) and <see cref="Smtp2GoMetrics"/>.
/// Logs the endpoint, method, region, status, <c>request_id</c> and elapsed time; never the API key and never a request or response body.
/// </summary>
public sealed partial class LoggerDiagnostics : ISmtp2GoDiagnostics
{
    /// <summary>The log category, <c>Scott.Mail.Smtp2Go</c>.</summary>
    public const string CategoryName = "Scott.Mail.Smtp2Go";

    private readonly ILogger _logger;
    private readonly Smtp2GoMetrics _metrics;

    /// <summary>Creates the diagnostics over a logger from <paramref name="loggerFactory"/> and <paramref name="metrics"/>.</summary>
    public LoggerDiagnostics(ILoggerFactory loggerFactory, Smtp2GoMetrics metrics)
    {
        Argument.ThrowIfNull(loggerFactory);
        Argument.ThrowIfNull(metrics);
        _logger = loggerFactory.CreateLogger(CategoryName);
        _metrics = metrics;
    }

    /// <inheritdoc />
    public void RequestStarting(Endpoint endpoint, Region? region)
    {
        Argument.ThrowIfNull(endpoint);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            string regionName = region?.ToString() ?? "custom";
            LogRequestStarting(_logger, endpoint.Method.Method, endpoint.Path, regionName);
        }
    }

    /// <inheritdoc />
    public void RequestCompleted(Endpoint endpoint, int statusCode, string? requestId, TimeSpan elapsed)
    {
        Argument.ThrowIfNull(endpoint);
        _metrics.RecordCompleted(endpoint, statusCode, elapsed);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            double elapsedMs = Math.Round(elapsed.TotalMilliseconds, 1);
            LogRequestCompleted(_logger, endpoint.Method.Method, endpoint.Path, statusCode, elapsedMs, requestId);
        }
    }

    /// <inheritdoc />
    public void RequestFailed(Endpoint endpoint, Exception exception, string? requestId)
    {
        Argument.ThrowIfNull(endpoint);
        Argument.ThrowIfNull(exception);
        _metrics.RecordFailed(endpoint, exception);
        int? statusCode = exception is Smtp2GoApiException api ? api.StatusCode : null;
        LogRequestFailed(_logger, exception, endpoint.Method.Method, endpoint.Path, statusCode, requestId);
    }

    /// <inheritdoc />
    public void ValidationFailed(Endpoint endpoint, IReadOnlyList<string> errors)
    {
        Argument.ThrowIfNull(endpoint);
        Argument.ThrowIfNull(errors);
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            LogValidationFailed(_logger, endpoint.Path, errors.Count, string.Join(" ", errors));
        }
    }

    /// <inheritdoc />
    public void SubaccountIdIgnored(Endpoint endpoint)
    {
        Argument.ThrowIfNull(endpoint);
        LogSubaccountIdIgnored(_logger, endpoint.Path);
    }

    /// <inheritdoc />
    public void EmailResult(int succeeded, int failed)
    {
        _metrics.RecordEmailResult(succeeded, failed);
        LogEmailResult(_logger, succeeded, failed);
    }

    [LoggerMessage(EventId = Smtp2GoEventIds.RequestStarting, Level = LogLevel.Debug, Message = "SMTP2GO {Method} {Endpoint} starting (region {Region})")]
    private static partial void LogRequestStarting(ILogger logger, string method, string endpoint, string region);

    [LoggerMessage(EventId = Smtp2GoEventIds.RequestCompleted, Level = LogLevel.Information, Message = "SMTP2GO {Method} {Endpoint} returned {StatusCode} in {ElapsedMs} ms (request_id {RequestId})")]
    private static partial void LogRequestCompleted(ILogger logger, string method, string endpoint, int statusCode, double elapsedMs, string? requestId);

    [LoggerMessage(EventId = Smtp2GoEventIds.EmailResult, Level = LogLevel.Information, Message = "SMTP2GO send accepted {Succeeded} and rejected {Failed} recipients")]
    private static partial void LogEmailResult(ILogger logger, int succeeded, int failed);

    [LoggerMessage(EventId = Smtp2GoEventIds.RequestFailed, Level = LogLevel.Warning, Message = "SMTP2GO {Method} {Endpoint} failed with status {StatusCode} (request_id {RequestId})")]
    private static partial void LogRequestFailed(ILogger logger, Exception exception, string method, string endpoint, int? statusCode, string? requestId);

    [LoggerMessage(EventId = Smtp2GoEventIds.ValidationFailed, Level = LogLevel.Warning, Message = "SMTP2GO {Endpoint} rejected by client-side validation with {ErrorCount} error(s): {Errors}")]
    private static partial void LogValidationFailed(ILogger logger, string endpoint, int errorCount, string errors);

    [LoggerMessage(EventId = Smtp2GoEventIds.SubaccountIdIgnored, Level = LogLevel.Debug, Message = "SMTP2GO {Endpoint} does not accept subaccount_id; the configured value was not sent")]
    private static partial void LogSubaccountIdIgnored(ILogger logger, string endpoint);
}
