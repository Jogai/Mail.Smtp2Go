namespace Scott.Mail.Smtp2Go.Tests.Integration.SmtpUsers;

/// <summary>users/smtp/view against the sandbox. Skipped without a sandbox key; see <see cref="SandboxKey"/>.</summary>
[Trait("Category", "Sandbox")]
public class SandboxSmtpUserTests
{
    [Fact]
    public async Task View_lists_the_users_and_the_default_rate_limit()
    {
        Smtp2GoClient client = SandboxKey.CreateClient();

        ApiResponse<SmtpUserViewResult> users = await client.SmtpUsers.ViewAsync(new SmtpUserViewRequest(), cancellationToken: TestContext.Current.CancellationToken);

        users.RequestId.Should().NotBeNullOrWhiteSpace();
        users.Data.Results.Should().NotBeNull();
        users.Data.DefaultRateLimitPeriod.Should().NotBeNullOrWhiteSpace();
        users.Data.Results.Should().OnlyContain(u => u.Username != null);
    }
}
