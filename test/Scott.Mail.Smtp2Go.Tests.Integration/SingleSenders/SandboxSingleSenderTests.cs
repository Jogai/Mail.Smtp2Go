namespace Scott.Mail.Smtp2Go.Tests.Integration.SingleSenders;

/// <summary>single_sender_emails/view against the sandbox. Skipped without a sandbox key; see <see cref="SandboxKey"/>.</summary>
[Trait("Category", "Sandbox")]
public class SandboxSingleSenderTests
{
    [Fact]
    public async Task View_lists_the_single_sender_addresses()
    {
        Smtp2GoClient client = SandboxKey.CreateClient();

        ApiResponse<SingleSenderViewResult> senders = await client.SingleSenders.ViewAsync(new SingleSenderViewRequest(), cancellationToken: TestContext.Current.CancellationToken);

        senders.RequestId.Should().NotBeNullOrWhiteSpace();
        senders.Data.Senders.Should().NotBeNull();
        senders.Data.Senders.Should().OnlyContain(s => s.EmailAddress != null && s.Verified != null);
    }
}
