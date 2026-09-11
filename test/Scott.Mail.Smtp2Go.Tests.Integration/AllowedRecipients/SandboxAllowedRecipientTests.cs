namespace Scott.Mail.Smtp2Go.Tests.Integration.AllowedRecipients;

/// <summary>allowed_recipients/view against the sandbox. Skipped without a sandbox key; see <see cref="SandboxKey"/>.</summary>
[Trait("Category", "Sandbox")]
public class SandboxAllowedRecipientTests
{
    [Fact]
    public async Task View_returns_the_list_and_whether_it_is_enabled()
    {
        Smtp2GoClient client = SandboxKey.CreateClient();

        ApiResponse<AllowedRecipientsList> list = await client.AllowedRecipients.ViewAsync(cancellationToken: TestContext.Current.CancellationToken);

        list.RequestId.Should().NotBeNullOrWhiteSpace();
        list.Data.AllowedRecipients.Should().NotBeNull();
        list.Data.Enabled.Should().NotBeNull();
    }
}
