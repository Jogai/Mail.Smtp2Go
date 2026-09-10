using System.Diagnostics;

namespace Scott.Mail.Smtp2Go.Transport;

/// <summary>
/// The <see cref="ActivitySource"/> this library emits one client span per API call from, tagged with <c>smtp2go.endpoint</c>, <c>smtp2go.region</c>,
/// <c>http.request.method</c>, <c>http.response.status_code</c> and <c>smtp2go.request_id</c>. Subscribe with OpenTelemetry via <c>AddSource(Smtp2GoActivitySource.Name)</c>.
/// </summary>
public static class Smtp2GoActivitySource
{
    /// <summary>The source name, <c>Scott.Mail.Smtp2Go</c>.</summary>
    public const string Name = "Scott.Mail.Smtp2Go";

    internal const string EndpointTag = "smtp2go.endpoint";
    internal const string RegionTag = "smtp2go.region";
    internal const string RequestIdTag = "smtp2go.request_id";
    internal const string HttpMethodTag = "http.request.method";
    internal const string HttpStatusTag = "http.response.status_code";

    internal static ActivitySource Instance { get; } = new(Name);
}
