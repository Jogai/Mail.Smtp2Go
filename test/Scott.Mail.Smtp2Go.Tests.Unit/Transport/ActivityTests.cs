using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Transport;

public class ActivityTests
{
    /// <summary>Other test classes run in parallel and emit spans from the same source, so each test listens for its own unique path only.</summary>
    private static ActivityListener Listen(string path, List<Activity> into)
    {
        ActivityListener listener = new()
        {
            ShouldListenTo = source => source.Name == Smtp2GoActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.DisplayName == path)
                {
                    lock (into)
                    {
                        into.Add(activity);
                    }
                }
            },
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    [Fact]
    public async Task One_client_span_per_call_tagged_with_endpoint_region_method_status_and_request_id()
    {
        const string path = "probe/activity-ok";
        List<Activity> activities = [];
        using ActivityListener listener = Listen(path, activities);
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond(path, HttpStatusCode.OK, """{"request_id":"trace-1","data":{}}""");
        Smtp2GoClient client = TestClient.Create(handler, o => o.Region = Region.EU);

        using JsonDocument _ = await client.Raw.SendJsonAsync(path, default);

        Activity activity = activities.Should().ContainSingle().Which;
        activity.Kind.Should().Be(ActivityKind.Client);
        activity.Status.Should().Be(ActivityStatusCode.Ok);
        activity.GetTagItem("smtp2go.endpoint").Should().Be(path);
        activity.GetTagItem("smtp2go.region").Should().Be("EU");
        activity.GetTagItem("http.request.method").Should().Be("POST");
        activity.GetTagItem("http.response.status_code").Should().Be(200);
        activity.GetTagItem("smtp2go.request_id").Should().Be("trace-1");
    }

    [Fact]
    public async Task Failed_calls_mark_the_span_as_error()
    {
        const string path = "probe/activity-error";
        List<Activity> activities = [];
        using ActivityListener listener = Listen(path, activities);
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond(path, HttpStatusCode.Unauthorized, Fixture.Read("Transport/error-flat.json"));
        Smtp2GoClient client = TestClient.Create(handler);

        Func<Task> act = async () => await client.Raw.SendJsonAsync(path, default);

        await act.Should().ThrowAsync<Smtp2GoAuthenticationException>();
        Activity activity = activities.Should().ContainSingle().Which;
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.GetTagItem("http.response.status_code").Should().Be(401);
        activity.GetTagItem("smtp2go.request_id").Should().Be("5f0a9c2e-1b3d-4e6f-8a7b-9c0d1e2f3a4b");
    }
}
