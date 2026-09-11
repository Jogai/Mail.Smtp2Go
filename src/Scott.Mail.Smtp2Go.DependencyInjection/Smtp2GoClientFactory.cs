using Microsoft.Extensions.Options;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.DependencyInjection;

/// <summary>Default <see cref="ISmtp2GoClientFactory"/>: named <see cref="IHttpClientFactory"/> client plus named <see cref="Smtp2GoOptions"/> plus the registered diagnostics.</summary>
internal sealed class Smtp2GoClientFactory : ISmtp2GoClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<Smtp2GoOptions> _options;
    private readonly ISmtp2GoDiagnostics _diagnostics;

    public Smtp2GoClientFactory(IHttpClientFactory httpClientFactory, IOptionsMonitor<Smtp2GoOptions> options, ISmtp2GoDiagnostics diagnostics)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _diagnostics = diagnostics;
    }

    public ISmtp2GoClient Create(string name)
    {
        Argument.ThrowIfNull(name);
        Smtp2GoOptions options = _options.Get(name);
        HttpClient http = _httpClientFactory.CreateClient(Smtp2GoHttpClientNames.For(name));
        return new Smtp2GoClient(http, options.ToClientOptions(), _diagnostics);
    }
}
