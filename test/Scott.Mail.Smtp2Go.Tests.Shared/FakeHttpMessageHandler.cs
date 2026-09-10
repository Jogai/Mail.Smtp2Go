using System.Net;
using System.Text;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Shared;

/// <summary>Records every request and replays canned responses matched by path suffix. Unmatched paths get a generic 200 envelope.</summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    public delegate Task<HttpResponseMessage> Responder(HttpRequestMessage request, CancellationToken cancellationToken);

    private readonly List<(string Path, Responder Respond)> _responders = [];

    public List<RecordedRequest> Requests { get; } = [];

    public Responder? Fallback { get; set; }

    public RecordedRequest LastRequest => Requests[^1];

    public FakeHttpMessageHandler Respond(string path, HttpStatusCode statusCode, string? body = null, Action<HttpResponseMessage>? configure = null)
    {
        return Respond(path, (_, _) =>
        {
            HttpResponseMessage response = Json(statusCode, body);
            configure?.Invoke(response);
            return Task.FromResult(response);
        });
    }

    public FakeHttpMessageHandler Respond(string path, Responder responder)
    {
        _responders.Add((path.TrimStart('/'), responder));
        return this;
    }

    public static HttpResponseMessage Json(HttpStatusCode statusCode, string? body)
    {
        HttpResponseMessage response = new(statusCode);
        if (body is not null)
        {
            response.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        return response;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(RecordedRequest.From(request, body));

        string path = request.RequestUri!.AbsolutePath;
        foreach ((string suffix, Responder respond) in _responders)
        {
            if (path.EndsWith("/" + suffix, StringComparison.Ordinal))
            {
                return await respond(request, cancellationToken);
            }
        }

        if (Fallback is not null)
        {
            return await Fallback(request, cancellationToken);
        }

        return Json(HttpStatusCode.OK, """{"request_id":"fake-request-id","data":{}}""");
    }
}

/// <summary>A snapshot of one request: everything is copied because the transport disposes the message after sending.</summary>
public sealed record RecordedRequest(
    HttpMethod Method,
    Uri Uri,
    IReadOnlyDictionary<string, string> Headers,
    string? ContentType,
    string? Body,
    Endpoint? Endpoint,
    bool AllowRetry)
{
    public static RecordedRequest From(HttpRequestMessage request, string? body)
    {
        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
        {
            headers[header.Key] = string.Join(",", header.Value);
        }

        return new RecordedRequest(
            request.Method,
            request.RequestUri!,
            headers,
            request.Content?.Headers.ContentType?.ToString(),
            body,
            RequestOptionKeys.TryGetEndpoint(request, out Endpoint? endpoint) ? endpoint : null,
            RequestOptionKeys.GetAllowRetry(request));
    }

    public string? Header(string name)
    {
        return Headers.TryGetValue(name, out string? value) ? value : null;
    }
}
