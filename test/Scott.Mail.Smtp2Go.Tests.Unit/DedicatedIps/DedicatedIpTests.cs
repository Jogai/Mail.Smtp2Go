using System.Net;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.DedicatedIps;

public class DedicatedIpTests
{
    [Fact]
    public void Dedicated_ips_view_is_seeded_without_subaccount_id()
    {
        Endpoint view = EndpointTable.Get("dedicated_ips/view");

        view.Should().Be(new Endpoint("dedicated_ips/view", HttpMethod.Post, Idempotent: true, AcceptsSubaccountId: false, RateLimitClass.None, Endpoint.DefaultMaxBodyBytes));
        EndpointTable.All.Where(e => e.Path.StartsWith("dedicated_ips/", StringComparison.Ordinal)).Should().ContainSingle();
    }

    [Fact]
    public void Docs_response_deserialises_with_an_empty_extra()
    {
        ApiResponse<IReadOnlyList<DedicatedIpPool>> viewed = JsonSerializer.Deserialize(Fixture.Read("DedicatedIps/view-response.json"), Smtp2GoJsonContext.Default.ApiResponseIReadOnlyListDedicatedIpPool)!;

        DedicatedIpPool pool = viewed.Data.Should().ContainSingle().Which;
        pool.Id.Should().Be(1234);
        pool.Name.Should().Be("Main Pool");
        pool.IpAddresses.Should().Equal("127.0.0.1");
        pool.Extra.Should().BeNull();
    }

    [Fact]
    public async Task ViewAsync_posts_an_empty_body_and_ignores_the_subaccount_id()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("dedicated_ips/view", HttpStatusCode.OK, Fixture.Read("DedicatedIps/view-response.json"));
        Smtp2GoClient client = TestClient.Create(handler, o => o.DefaultSubaccountId = "sub-1");

        (await client.DedicatedIps.ViewAsync()).Data.Should().HaveCount(1);

        handler.LastRequest.Endpoint!.Path.Should().Be("dedicated_ips/view");
        handler.LastRequest.Body.Should().Be("{}");
    }
}
