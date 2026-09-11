namespace Scott.Mail.Smtp2Go.Tests.Integration.Activity;

/// <summary>activity/search against the sandbox. Skipped without a sandbox key; see <see cref="SandboxKey"/>.</summary>
[Trait("Category", "Sandbox")]
public class SandboxActivityTests
{
    [Fact]
    public async Task Search_returns_200()
    {
        Smtp2GoClient client = SandboxKey.CreateClient();

        ApiResponse<ActivitySearchResult> response = await client.Activity.SearchAsync(new ActivitySearchRequest { Limit = 10, StartDate = DateTimeOffset.UtcNow.AddDays(-7) }, cancellationToken: TestContext.Current.CancellationToken);

        response.RequestId.Should().NotBeNullOrWhiteSpace();
        response.Data.Events.Should().NotBeNull();
        response.Data.TotalEvents.Should().NotBeNull();
    }
}
