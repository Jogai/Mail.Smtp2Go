using System.Net.Http.Headers;

namespace Scott.Mail.Smtp2Go.SpecHarvester;

/// <summary>
/// Fetches documentation pages by site-relative path from the developer site, or from a directory that mirrors it. With a cache
/// directory, downloaded pages are saved there and later runs read them from disk, which keeps the tool usable offline.
/// </summary>
public sealed class PageSource : IDisposable
{
    private const string UserAgent = "Scott.Mail.Smtp2Go.SpecHarvester/1.0 (+https://github.com/Jogai/Mail.Smtp2Go)";
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    private readonly Uri? _baseUrl;
    private readonly string? _directory;
    private readonly string? _cacheDirectory;
    private readonly HttpClient? _http;
    private readonly SemaphoreSlim _throttle = new(4);

    /// <summary>Creates a source that reads from <paramref name="source"/>: an <c>http(s)</c> URL or a local directory.</summary>
    /// <param name="source">The site root (<c>https://developers.smtp2go.com/</c>) or a directory laid out the same way.</param>
    /// <param name="cacheDirectory">Optional directory to read pages from when present and to save downloads to.</param>
    public PageSource(string source, string? cacheDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        _cacheDirectory = cacheDirectory;
        if (Uri.TryCreate(source, UriKind.Absolute, out Uri? url) && (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps))
        {
            _baseUrl = url.AbsolutePath.EndsWith('/') ? url : new Uri(url + "/");
            _http = new HttpClient { Timeout = s_timeout };
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/markdown"));
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.5));
        }
        else
        {
            _directory = Path.GetFullPath(source);
        }
    }

    /// <summary>A label for messages.</summary>
    public string Description => _baseUrl?.ToString() ?? _directory!;

    /// <summary>Returns the text of the page at <paramref name="relativePath"/> (for example <c>reference/send-standard-email.md</c>).</summary>
    public async Task<string> GetAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        string relative = relativePath.TrimStart('/');

        if (_cacheDirectory is not null)
        {
            string cached = Path.Combine(_cacheDirectory, relative);
            if (File.Exists(cached))
            {
                return await File.ReadAllTextAsync(cached, cancellationToken).ConfigureAwait(false);
            }
        }

        string text;
        if (_directory is not null)
        {
            text = await File.ReadAllTextAsync(Path.Combine(_directory, relative), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _throttle.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using HttpResponseMessage response = await _http!.GetAsync(new Uri(_baseUrl!, relative), cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _throttle.Release();
            }
        }

        if (_cacheDirectory is not null)
        {
            string cached = Path.Combine(_cacheDirectory, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(cached)!);
            await File.WriteAllTextAsync(cached, text, cancellationToken).ConfigureAwait(false);
        }

        return text;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _http?.Dispose();
        _throttle.Dispose();
    }
}
