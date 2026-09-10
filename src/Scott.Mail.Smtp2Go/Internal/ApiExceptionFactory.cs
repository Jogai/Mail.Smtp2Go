using System.Net;
using System.Net.Http.Headers;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Maps a non-success response to the exception hierarchy (architecture.md section 5.2).</summary>
internal static class ApiExceptionFactory
{
    private const int RawBodyExcerptLength = 200;

    public static Smtp2GoApiException Create(Endpoint endpoint, HttpStatusCode statusCode, string? reasonPhrase, ApiError error, TimeSpan? retryAfter)
    {
        int status = (int)statusCode;
        string detail = error.Message ?? error.ErrorCode ?? Excerpt(error.RawBody) ?? "no response body";
        string reason = string.IsNullOrEmpty(reasonPhrase) ? string.Empty : " " + reasonPhrase;
        string message = FormattableString.Invariant($"SMTP2GO API request to '{endpoint.Path}' failed with status {status}{reason}: {detail}");

        if (statusCode == HttpStatusCode.Unauthorized)
        {
            return new Smtp2GoAuthenticationException(message, status, endpoint.Path, error);
        }

        if ((int)statusCode == 429)
        {
            return new Smtp2GoRateLimitException(message, status, endpoint.Path, error, retryAfter);
        }

        if (statusCode == HttpStatusCode.Forbidden || error.Code == Smtp2GoErrorCode.EndpointPermissionDenied)
        {
            string permissionMessage = FormattableString.Invariant($"{message} The API key does not have permission to call '{endpoint.Path}'.");
            return new Smtp2GoPermissionException(permissionMessage, status, endpoint.Path, error);
        }

        return new Smtp2GoApiException(message, status, endpoint.Path, error);
    }

    /// <summary>Reads <c>Retry-After</c> as a delay or an absolute date; never negative.</summary>
    public static TimeSpan? GetRetryAfter(HttpResponseHeaders headers)
    {
        RetryConditionHeaderValue? retryAfter = headers.RetryAfter;
        if (retryAfter is null)
        {
            return null;
        }

        if (retryAfter.Delta is { } delta)
        {
            return delta < TimeSpan.Zero ? TimeSpan.Zero : delta;
        }

        if (retryAfter.Date is { } date)
        {
            TimeSpan remaining = date - DateTimeOffset.UtcNow;
            return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }

        return null;
    }

    private static string? Excerpt(string? rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return null;
        }

        string trimmed = rawBody!.Trim();
        return trimmed.Length <= RawBodyExcerptLength ? trimmed : trimmed.Remove(RawBodyExcerptLength) + "…";
    }
}
