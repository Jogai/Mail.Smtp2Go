using System.Diagnostics.Metrics;
using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scott.Mail.Smtp2Go.DependencyInjection;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.DependencyInjection;

public sealed class DiagnosticsTests
{
    private const string Endpoint = "email/search";

    [Fact]
    public async Task Logs_contain_endpoint_and_request_id_but_never_the_key()
    {
        using DiTestHost host = DiTestHost.Create();
        host.Handler.Respond(Endpoint, HttpStatusCode.OK, """{"request_id":"req-42","data":{}}""");

        using JsonDocument document = await host.Client().Raw.SendJsonAsync(Endpoint, default, cancellationToken: TestContext.Current.CancellationToken);

        CapturingLoggerProvider.LogEntry[] entries = [.. host.Logs.Entries.Where(e => e.Category == LoggerDiagnostics.CategoryName)];
        entries.Should().Contain(e => e.EventId.Id == Smtp2GoEventIds.RequestStarting && e.Level == LogLevel.Debug && e.Message.Contains(Endpoint) && e.Message.Contains("Global"));
        entries.Should().Contain(e => e.EventId.Id == Smtp2GoEventIds.RequestCompleted && e.Level == LogLevel.Information && e.Message.Contains(Endpoint) && e.Message.Contains("req-42") && e.Message.Contains("200"));
        host.Logs.Entries.Should().NotContain(e => e.Message.Contains(TestClient.ApiKey, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Failed_requests_are_logged_with_status_and_exception()
    {
        using DiTestHost host = DiTestHost.Create(options => options.Resilience.MaxRetries = 0);
        host.Handler.Respond(Endpoint, HttpStatusCode.InternalServerError, """{"request_id":"req-err","data":{"error":"boom"}}""");

        Func<Task> act = () => host.Client().Raw.SendJsonAsync(Endpoint, default, cancellationToken: TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<Smtp2GoApiException>();
        host.Logs.Entries.Should().Contain(e =>
            e.EventId.Id == Smtp2GoEventIds.RequestFailed && e.Level == LogLevel.Warning && e.Exception is Smtp2GoApiException
            && e.Message.Contains(Endpoint) && e.Message.Contains("500") && e.Message.Contains("req-err"));
    }

    [Fact]
    public async Task Validation_failures_and_ignored_subaccounts_are_logged()
    {
        using DiTestHost host = DiTestHost.Create(options => options.DefaultSubaccountId = "sub-1");
        ISmtp2GoClient client = host.Client();

        using JsonDocument document = await client.Raw.SendJsonAsync("email/send", default, cancellationToken: TestContext.Current.CancellationToken);
        Func<Task> oversized = () => client.Raw.SendJsonAsync("stats/email_cycle", TestClient.Json("{\"pad\":\"" + new string('x', 1024 * 1024) + "\"}"), cancellationToken: TestContext.Current.CancellationToken);

        await oversized.Should().ThrowAsync<Smtp2GoValidationException>();
        host.Logs.Entries.Should().Contain(e => e.EventId.Id == Smtp2GoEventIds.SubaccountIdIgnored && e.Message.Contains("email/send"));
        host.Logs.Entries.Should().Contain(e => e.EventId.Id == Smtp2GoEventIds.ValidationFailed && e.Message.Contains("stats/email_cycle") && e.Message.Contains("bytes"));
    }

    [Fact]
    public void Log_templates_never_contain_the_key_or_a_body()
    {
        List<(MethodInfo Method, LoggerMessageAttribute Attribute)> messages = [];
        foreach (Type type in typeof(LoggerDiagnostics).Assembly.GetTypes())
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.GetCustomAttribute<LoggerMessageAttribute>() is { } attribute)
                {
                    messages.Add((method, attribute));
                }
            }
        }

        messages.Should().HaveCountGreaterThanOrEqualTo(12, "every diagnostic and resilience event has a [LoggerMessage]");
        foreach ((MethodInfo method, LoggerMessageAttribute attribute) in messages)
        {
            attribute.Message.Should().NotContainEquivalentOf("{ApiKey}").And.NotContainEquivalentOf("{Key}").And.NotContainEquivalentOf("{Body}").And.NotContainEquivalentOf("{Payload}");
            attribute.Message.Should().NotContainEquivalentOf("api-");
            method.GetParameters().Select(p => p.Name).Should().NotContain(p => p!.Contains("key", StringComparison.OrdinalIgnoreCase) || p.Contains("body", StringComparison.OrdinalIgnoreCase));
            attribute.EventId.Should().BeInRange(100, 499);
        }

        messages.Select(m => m.Attribute.EventId).Should().OnlyHaveUniqueItems("event ids are stable identifiers");
        messages.Select(m => m.Attribute.EventId).Should().BeEquivalentTo(
            typeof(Smtp2GoEventIds).GetFields(BindingFlags.Public | BindingFlags.Static).Select(f => (int)f.GetRawConstantValue()!),
            "Smtp2GoEventIds documents exactly the ids in use");
    }

    [Fact]
    public async Task Metrics_are_observed_after_one_call()
    {
        List<(string Instrument, long Value, Dictionary<string, object?> Tags)> counters = [];
        List<(string Instrument, double Value, Dictionary<string, object?> Tags)> histograms = [];
        using MeterListener listener = new();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == Smtp2GoMetrics.MeterName)
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            lock (counters)
            {
                counters.Add((instrument.Name, value, ToDictionary(tags)));
            }
        });
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
        {
            lock (histograms)
            {
                histograms.Add((instrument.Name, value, ToDictionary(tags)));
            }
        });
        listener.Start();

        using DiTestHost host = DiTestHost.Create(options => options.Resilience.MaxRetries = 0);
        host.Handler.Respond(Endpoint, HttpStatusCode.OK, """{"request_id":"req-1","data":{}}""");
        host.Handler.Respond("email/send", HttpStatusCode.BadRequest, """{"request_id":"req-2","data":{"error":"bad"}}""");
        ISmtp2GoClient client = host.Client();

        using JsonDocument document = await client.Raw.SendJsonAsync(Endpoint, default, cancellationToken: TestContext.Current.CancellationToken);
        Func<Task> failing = () => client.Raw.SendJsonAsync("email/send", default, cancellationToken: TestContext.Current.CancellationToken);
        await failing.Should().ThrowAsync<Smtp2GoApiException>();
        host.Provider.GetRequiredService<ISmtp2GoDiagnostics>().EmailResult(succeeded: 3, failed: 1);

        counters.Should().Contain(c => c.Instrument == Smtp2GoMetrics.RequestsInstrument && c.Value == 1
            && Equals(c.Tags[Smtp2GoMetrics.EndpointTag], Endpoint) && Equals(c.Tags[Smtp2GoMetrics.StatusCodeTag], 200));
        counters.Should().Contain(c => c.Instrument == Smtp2GoMetrics.RequestsInstrument && c.Value == 1
            && Equals(c.Tags[Smtp2GoMetrics.EndpointTag], "email/send") && Equals(c.Tags[Smtp2GoMetrics.StatusCodeTag], 400) && Equals(c.Tags[Smtp2GoMetrics.ErrorTypeTag], nameof(Smtp2GoApiException)));
        histograms.Should().ContainSingle(h => h.Instrument == Smtp2GoMetrics.RequestDurationInstrument && Equals(h.Tags[Smtp2GoMetrics.EndpointTag], Endpoint))
            .Which.Value.Should().BeGreaterThanOrEqualTo(0);
        counters.Should().Contain(c => c.Instrument == Smtp2GoMetrics.EmailAcceptedInstrument && c.Value == 3);
        counters.Should().Contain(c => c.Instrument == Smtp2GoMetrics.EmailFailedInstrument && c.Value == 1);
    }

    private static Dictionary<string, object?> ToDictionary(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        Dictionary<string, object?> result = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, object?> tag in tags)
        {
            result[tag.Key] = tag.Value;
        }

        return result;
    }
}
