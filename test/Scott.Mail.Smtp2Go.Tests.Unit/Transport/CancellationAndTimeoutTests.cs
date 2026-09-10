using System.Text.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Transport;

public class CancellationAndTimeoutTests
{
    [Fact]
    public async Task Already_cancelled_token_propagates_as_OperationCanceledException()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        Func<Task> act = async () => await client.Raw.SendJsonAsync("stats/email_cycle", default, cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Cancellation_during_the_request_propagates_unwrapped_and_is_not_reported_as_a_failure()
    {
        using CancellationTokenSource cts = new();
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("stats/email_cycle", async (_, ct) =>
        {
            await cts.CancelAsync();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return FakeHttpMessageHandler.Json(System.Net.HttpStatusCode.OK, "{}");
        });
        RecordingDiagnostics diagnostics = new();
        Smtp2GoClient client = TestClient.Create(handler, diagnostics: diagnostics);

        Func<Task> act = async () => await client.Raw.SendJsonAsync("stats/email_cycle", default, cancellationToken: cts.Token);

        OperationCanceledException exception = (await act.Should().ThrowAsync<OperationCanceledException>()).Which;
        exception.Should().NotBeOfType<TimeoutException>();
        diagnostics.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task Request_timeout_becomes_TimeoutException_with_the_cancellation_as_inner()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("stats/email_cycle", async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
            return FakeHttpMessageHandler.Json(System.Net.HttpStatusCode.OK, "{}");
        });
        RecordingDiagnostics diagnostics = new();
        Smtp2GoClient client = TestClient.Create(handler, diagnostics: diagnostics);

        Func<Task> act = async () => await client.Raw.SendJsonAsync("stats/email_cycle", default, options: new RequestOptions { Timeout = TimeSpan.FromMilliseconds(50) });

        TimeoutException exception = (await act.Should().ThrowAsync<TimeoutException>()).Which;
        exception.InnerException.Should().BeAssignableTo<OperationCanceledException>();
        exception.Message.Should().Contain("stats/email_cycle");
        diagnostics.Failed.Should().ContainSingle().Which.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public async Task Options_timeout_applies_when_the_request_sets_none()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("stats/email_cycle", async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
            return FakeHttpMessageHandler.Json(System.Net.HttpStatusCode.OK, "{}");
        });
        Smtp2GoClient client = TestClient.Create(handler, o => o.Timeout = TimeSpan.FromMilliseconds(50));

        Func<Task> act = async () => await client.Raw.SendJsonAsync("stats/email_cycle", default);

        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task Infinite_timeout_does_not_create_a_deadline()
    {
        FakeHttpMessageHandler handler = new();
        Smtp2GoClient client = TestClient.Create(handler, o => o.Timeout = Timeout.InfiniteTimeSpan);

        using JsonDocument document = await client.Raw.SendJsonAsync("stats/email_cycle", default, options: new RequestOptions { Timeout = Timeout.InfiniteTimeSpan });

        document.RootElement.GetProperty("request_id").GetString().Should().Be("fake-request-id");
    }
}
