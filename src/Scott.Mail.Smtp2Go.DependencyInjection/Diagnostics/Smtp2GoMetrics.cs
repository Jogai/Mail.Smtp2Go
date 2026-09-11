using System.Diagnostics;
using System.Diagnostics.Metrics;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.DependencyInjection;

/// <summary>
/// The <see cref="Meter"/> <c>Scott.Mail.Smtp2Go</c>: <c>smtp2go.client.requests</c> (counter, tags <c>smtp2go.endpoint</c>, <c>http.response.status_code</c>,
/// <c>error.type</c>), <c>smtp2go.client.request.duration</c> (histogram in seconds, same tags), <c>smtp2go.email.accepted</c> and <c>smtp2go.email.failed</c>
/// (recipient counters). Subscribe with OpenTelemetry's <c>AddMeter(Smtp2GoMetrics.MeterName)</c>. Registered as a singleton by <c>AddSmtp2Go</c>
/// and fed by <c>LoggerDiagnostics</c>.
/// </summary>
public sealed class Smtp2GoMetrics
{
    /// <summary>The meter name, <c>Scott.Mail.Smtp2Go</c>.</summary>
    public const string MeterName = "Scott.Mail.Smtp2Go";

    /// <summary>Counter of API calls, successful or not.</summary>
    public const string RequestsInstrument = "smtp2go.client.requests";

    /// <summary>Histogram of call duration in seconds, recorded for calls that received a response.</summary>
    public const string RequestDurationInstrument = "smtp2go.client.request.duration";

    /// <summary>Counter of recipients accepted by send calls.</summary>
    public const string EmailAcceptedInstrument = "smtp2go.email.accepted";

    /// <summary>Counter of recipients rejected by send calls.</summary>
    public const string EmailFailedInstrument = "smtp2go.email.failed";

    /// <summary>Tag carrying the endpoint path, for example <c>email/send</c>.</summary>
    public const string EndpointTag = "smtp2go.endpoint";

    /// <summary>Tag carrying the HTTP status code, when a response was received.</summary>
    public const string StatusCodeTag = "http.response.status_code";

    /// <summary>Tag carrying the exception type name of a failed call.</summary>
    public const string ErrorTypeTag = "error.type";

    private readonly Counter<long> _requests;
    private readonly Histogram<double> _duration;
    private readonly Counter<long> _accepted;
    private readonly Counter<long> _failed;

    /// <summary>Creates the meter through <paramref name="meterFactory"/>, which owns its lifetime.</summary>
    public Smtp2GoMetrics(IMeterFactory meterFactory)
    {
        Argument.ThrowIfNull(meterFactory);
        Meter meter = meterFactory.Create(MeterName);
        _requests = meter.CreateCounter<long>(RequestsInstrument, "{request}", "API calls made by the SMTP2GO client.");
        _duration = meter.CreateHistogram<double>(RequestDurationInstrument, "s", "Duration of SMTP2GO API calls that received a response.");
        _accepted = meter.CreateCounter<long>(EmailAcceptedInstrument, "{recipient}", "Recipients accepted by SMTP2GO send calls.");
        _failed = meter.CreateCounter<long>(EmailFailedInstrument, "{recipient}", "Recipients rejected by SMTP2GO send calls.");
    }

    internal void RecordCompleted(Endpoint endpoint, int statusCode, TimeSpan elapsed)
    {
        TagList tags = new()
        {
            { EndpointTag, endpoint.Path },
            { StatusCodeTag, statusCode },
        };
        _requests.Add(1, tags);
        _duration.Record(elapsed.TotalSeconds, tags);
    }

    internal void RecordFailed(Endpoint endpoint, Exception exception)
    {
        TagList tags = new()
        {
            { EndpointTag, endpoint.Path },
            { ErrorTypeTag, exception.GetType().Name },
        };
        if (exception is Smtp2GoApiException api)
        {
            tags.Add(StatusCodeTag, api.StatusCode);
        }

        _requests.Add(1, tags);
    }

    internal void RecordEmailResult(int succeeded, int failed)
    {
        if (succeeded > 0)
        {
            _accepted.Add(succeeded);
        }

        if (failed > 0)
        {
            _failed.Add(failed);
        }
    }
}
