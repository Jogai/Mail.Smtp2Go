using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// Entry point of the library. Construct it with an API key or options to use one shared, process-wide <see cref="HttpClient"/>, or pass your own
/// <see cref="HttpClient"/> (the dependency injection package does this through <c>IHttpClientFactory</c>, which is the preferred path in hosted applications).
/// A caller-supplied <see cref="HttpClient"/> is never mutated: base address, headers and timeout are applied per request.
/// </summary>
public sealed class Smtp2GoClient : ISmtp2GoClient
{
    private static readonly TimeSpan s_pooledConnectionLifetime = TimeSpan.FromMinutes(15);
    private static readonly Lazy<HttpClient> s_sharedHttpClient = new(CreateSharedHttpClient, LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly Smtp2GoConnection _connection;

    /// <summary>Creates a client for <paramref name="apiKey"/> using the shared <see cref="HttpClient"/>.</summary>
    public Smtp2GoClient(string apiKey)
        : this(CreateOptions(apiKey))
    {
    }

    /// <summary>Creates a client from <paramref name="options"/> using the shared <see cref="HttpClient"/>.</summary>
    /// <exception cref="Smtp2GoValidationException"><paramref name="options"/> is not valid.</exception>
    public Smtp2GoClient(Smtp2GoClientOptions options)
        : this(s_sharedHttpClient.Value, options, null)
    {
    }

    /// <summary>Creates a client over a caller-owned <paramref name="httpClient"/>. The client is not disposed by this instance.</summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to send with; typically factory-managed.</param>
    /// <param name="options">The options; validated with <see cref="Smtp2GoClientOptions.Validate"/>.</param>
    /// <param name="diagnostics">Receives per-call notifications; <see langword="null"/> for none.</param>
    /// <exception cref="Smtp2GoValidationException"><paramref name="options"/> is not valid.</exception>
    public Smtp2GoClient(HttpClient httpClient, Smtp2GoClientOptions options, ISmtp2GoDiagnostics? diagnostics = null)
    {
        Argument.ThrowIfNull(httpClient);
        Argument.ThrowIfNull(options);
        options.Validate();

        Options = options;
        _connection = new Smtp2GoConnection(httpClient, options, diagnostics);
        Raw = new RawClient(_connection);
        Email = new EmailClient(_connection);
        Webhooks = new WebhookClient(_connection);
    }

    /// <summary>The options this client was created with.</summary>
    public Smtp2GoClientOptions Options { get; }

    /// <inheritdoc />
    public IEmailClient Email { get; }

    /// <inheritdoc />
    public IWebhookClient Webhooks { get; }

    /// <inheritdoc />
    public IRawClient Raw { get; }

    // Family clients (Stats, Webhooks, ...) are added here by their plans as properties over _connection.

    private static Smtp2GoClientOptions CreateOptions(string apiKey)
    {
        Argument.ThrowIfNullOrWhiteSpace(apiKey);
        return new Smtp2GoClientOptions { ApiKey = apiKey };
    }

    /// <summary>The only place in the library that constructs an <see cref="HttpClient"/>: one per process, with connection pooling that honours DNS changes.</summary>
    private static HttpClient CreateSharedHttpClient()
    {
#if NET8_0_OR_GREATER
        SocketsHttpHandler handler = new() { PooledConnectionLifetime = s_pooledConnectionLifetime };
#else
        HttpClientHandler handler = new();
#endif
        return new HttpClient(handler, disposeHandler: true) { Timeout = Timeout.InfiniteTimeSpan };
    }
}
