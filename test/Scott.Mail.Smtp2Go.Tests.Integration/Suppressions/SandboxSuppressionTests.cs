namespace Scott.Mail.Smtp2Go.Tests.Integration.Suppressions;

/// <summary>suppression/add, view, remove against the sandbox. Skipped without a sandbox key; see <see cref="SandboxKey"/>.</summary>
[Trait("Category", "Sandbox")]
public class SandboxSuppressionTests
{
    [Fact]
    public async Task Add_view_remove_round_trip()
    {
        Smtp2GoClient client = SandboxKey.CreateClient();
        CancellationToken ct = TestContext.Current.CancellationToken;
        string address = $"suppressed-{Guid.NewGuid():N}@example.com";

        ApiResponse<SuppressionAddResult> added = await client.Suppressions.AddAsync(new SuppressionAddRequest { EmailAddress = address, BlockDescription = "Scott.Mail.Smtp2Go integration" }, cancellationToken: ct);
        try
        {
            added.Data.EmailAddress.Should().Be(address);
            added.Data.Added.Should().BeTrue();

            ApiResponse<SuppressionViewResult> viewed = await client.Suppressions.ViewAsync(new SuppressionViewRequest { EmailAddress = address }, cancellationToken: ct);
            viewed.Data.Results.Should().Contain(s => s.EmailAddress == address && s.Reason == SuppressionType.Manual);
        }
        finally
        {
            ApiResponse<SuppressionRemoveResult> removed = await client.Suppressions.RemoveAsync(new SuppressionRemoveRequest { EmailAddress = address, Reasons = [SuppressionType.Manual] }, cancellationToken: ct);
            removed.Data.Suppressions.Should().Contain(s => s.Reason == SuppressionType.Manual && s.Removed == true);
        }
    }
}
