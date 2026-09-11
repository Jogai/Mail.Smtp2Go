namespace Scott.Mail.Smtp2Go.Tests.Integration.DedicatedIps;

/// <summary>dedicated_ips/view against the sandbox. Skipped without a sandbox key; see <see cref="SandboxKey"/>.</summary>
[Trait("Category", "Sandbox")]
public class SandboxDedicatedIpTests
{
    [Fact]
    public async Task View_lists_the_pools()
    {
        Smtp2GoClient client = SandboxKey.CreateClient();

        ApiResponse<IReadOnlyList<DedicatedIpPool>> pools = await client.DedicatedIps.ViewAsync(cancellationToken: TestContext.Current.CancellationToken);

        pools.RequestId.Should().NotBeNullOrWhiteSpace();
        pools.Data.Should().NotBeNull();
        pools.Data.Should().OnlyContain(p => p.Id != null && p.IpAddresses != null);
    }
}
