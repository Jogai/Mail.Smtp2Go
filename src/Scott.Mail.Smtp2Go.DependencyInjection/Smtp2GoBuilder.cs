using Microsoft.Extensions.DependencyInjection;

namespace Scott.Mail.Smtp2Go.DependencyInjection;

internal sealed class Smtp2GoBuilder : ISmtp2GoBuilder
{
    public Smtp2GoBuilder(string name, IServiceCollection services, IHttpClientBuilder httpClientBuilder)
    {
        Name = name;
        Services = services;
        HttpClientBuilder = httpClientBuilder;
    }

    public string Name { get; }

    public IServiceCollection Services { get; }

    public IHttpClientBuilder HttpClientBuilder { get; }
}
