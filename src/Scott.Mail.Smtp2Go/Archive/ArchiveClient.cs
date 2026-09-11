using System.Net;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>Default <see cref="IArchiveClient"/> over the shared connection.</summary>
internal sealed class ArchiveClient(Smtp2GoConnection connection) : IArchiveClient
{
    private const int CopyBufferSize = 81920;
    private static readonly Endpoint s_search = EndpointTable.Get("archive/search");
    private static readonly Endpoint s_email = EndpointTable.Get("archive/email");

    public Task<ApiResponse<ArchiveSearchResult>> SearchAsync(ArchiveSearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return connection.SendAsync<ArchiveSearchRequest, ArchiveSearchResult>(s_search, request, options, cancellationToken);
    }

    public IAsyncEnumerable<ArchivedEmail> SearchAllAsync(ArchiveSearchRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(request);
        return ContinueTokenPager.EnumerateAsync(
            request.ContinueToken,
            async (token, ct) =>
            {
                ApiResponse<ArchiveSearchResult> response = await SearchAsync(request with { ContinueToken = token }, options, ct).ConfigureAwait(false);
                return new ContinueTokenPager.Page<ArchivedEmail>(response.Data?.Emails, response.Data?.ContinueToken);
            },
            cancellationToken);
    }

    public Task<ApiResponse<ArchivedEmail>> GetAsync(string emailId, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNullOrWhiteSpace(emailId);
        return connection.SendAsync<ArchiveEmailRequest, ArchivedEmail>(s_email, new ArchiveEmailRequest { EmailId = emailId }, options, cancellationToken);
    }

    public async Task<long> DownloadOriginalAsync(ArchivedEmail email, Stream destination, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNull(email);
        Argument.ThrowIfNull(destination);
        if (string.IsNullOrWhiteSpace(email.Url) || !Uri.TryCreate(email.Url, UriKind.Absolute, out Uri? url) || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("The archived email has no absolute http(s) download url.", nameof(email));
        }

        using HttpResponseMessage response = await FetchAsync(url, options, cancellationToken).ConfigureAwait(false);
        using Stream source = await Smtp2GoConnection.ReadContentStreamAsync(response.Content, cancellationToken).ConfigureAwait(false);
        long before = destination.CanSeek ? destination.Position : 0;
        await source.CopyToAsync(destination, CopyBufferSize, cancellationToken).ConfigureAwait(false);
        if (destination.CanSeek)
        {
            return destination.Position - before;
        }

        return response.Content.Headers.ContentLength ?? -1;
    }

    /// <summary>GETs the url without credentials, then with the API key when the server demands one.</summary>
    private async Task<HttpResponseMessage> FetchAsync(Uri url, RequestOptions? options, CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await connection.GetAsync(url, options, withApiKey: false, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            response.Dispose();
            response = await connection.GetAsync(url, options, withApiKey: true, cancellationToken).ConfigureAwait(false);
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        int status = (int)response.StatusCode;
        response.Dispose();
        throw new Smtp2GoApiException(FormattableString.Invariant($"Downloading the archived email from '{url}' returned status {status}."), status, url.AbsolutePath, null);
    }
}
