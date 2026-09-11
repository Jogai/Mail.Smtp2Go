using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Scott.Mail.Smtp2Go.DependencyInjection;
using Scott.Mail.Smtp2Go.Tests.Shared;

namespace Scott.Mail.Smtp2Go.Tests.Unit.DependencyInjection;

/// <summary>A service provider with AddSmtp2Go wired to a <see cref="FakeHttpMessageHandler"/>, a <see cref="FakeTimeProvider"/> and a capturing logger.</summary>
internal sealed class DiTestHost : IDisposable
{
    private DiTestHost(ServiceProvider provider, FakeHttpMessageHandler handler, FakeTimeProvider time, CapturingLoggerProvider logs)
    {
        Provider = provider;
        Handler = handler;
        Time = time;
        Logs = logs;
    }

    public ServiceProvider Provider { get; }

    public FakeHttpMessageHandler Handler { get; }

    public FakeTimeProvider Time { get; }

    public CapturingLoggerProvider Logs { get; }

    public static DiTestHost Create(Action<Smtp2GoOptions>? configure = null, Action<IServiceCollection>? services = null, string? name = null)
    {
        FakeHttpMessageHandler handler = new();
        FakeTimeProvider time = new();
        CapturingLoggerProvider logs = new();

        ServiceCollection collection = new();
        collection.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Trace).AddProvider(logs));
        collection.AddSingleton<TimeProvider>(time);

        Action<Smtp2GoOptions> configureAll = options =>
        {
            options.ApiKey = TestClient.ApiKey;
            configure?.Invoke(options);
        };
        ISmtp2GoBuilder builder = name is null ? collection.AddSmtp2Go(configureAll) : collection.AddSmtp2Go(name, configureAll);
        builder.HttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => handler);
        services?.Invoke(collection);

        return new DiTestHost(collection.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }), handler, time, logs);
    }

    public ISmtp2GoClient Client(string? name = null)
    {
        return Provider.GetRequiredService<ISmtp2GoClientFactory>().Create(name ?? Options.DefaultName);
    }

    /// <summary>Runs <paramref name="operation"/> while advancing the fake clock so Polly's delays and timers fire.</summary>
    public async Task<T> RunAsync<T>(Func<Task<T>> operation, TimeSpan? step = null)
    {
        Task<T> task = operation();
        TimeSpan increment = step ?? TimeSpan.FromSeconds(1);
        int iterations = 0;
        while (!task.IsCompleted)
        {
            await Task.Delay(5, TestContext.Current.CancellationToken);
            Time.Advance(increment);
            if (++iterations > 2000)
            {
                throw new TimeoutException("The operation did not complete while advancing fake time.");
            }
        }

        return await task;
    }

    public void Dispose()
    {
        Provider.Dispose();
    }
}
