namespace Scott.Mail.Smtp2Go.Tests.Integration.AllowedSenders;

/// <summary>allowed_senders/view against the sandbox. Skipped without a sandbox key; see <see cref="SandboxKey"/>.</summary>
[Trait("Category", "Sandbox")]
public class SandboxAllowedSenderTests
{
    [Fact]
    public async Task View_returns_the_list_and_its_mode()
    {
        Smtp2GoClient client = SandboxKey.CreateClient();

        ApiResponse<AllowedSendersList> list = await client.AllowedSenders.ViewAsync(cancellationToken: TestContext.Current.CancellationToken);

        list.RequestId.Should().NotBeNullOrWhiteSpace();
        list.Data.AllowedSenders.Should().NotBeNull();
        list.Data.Mode.Should().NotBeNull().And.NotBe(AllowedSendersMode.Unknown);
    }
}
