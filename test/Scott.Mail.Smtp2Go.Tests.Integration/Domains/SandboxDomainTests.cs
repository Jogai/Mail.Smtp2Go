namespace Scott.Mail.Smtp2Go.Tests.Integration.Domains;

/// <summary>domain/view against the sandbox. Skipped without a sandbox key; see <see cref="SandboxKey"/>.</summary>
[Trait("Category", "Sandbox")]
public class SandboxDomainTests
{
    [Fact]
    public async Task View_lists_the_sender_domains()
    {
        Smtp2GoClient client = SandboxKey.CreateClient();

        ApiResponse<DomainViewResult> domains = await client.Domains.ViewAsync(new DomainViewRequest(), cancellationToken: TestContext.Current.CancellationToken);

        domains.RequestId.Should().NotBeNullOrWhiteSpace();
        domains.Data.Domains.Should().NotBeNull();
        domains.Data.Domains.Should().OnlyContain(d => d.Domain != null && d.Domain.FullDomain != null);
    }
}
