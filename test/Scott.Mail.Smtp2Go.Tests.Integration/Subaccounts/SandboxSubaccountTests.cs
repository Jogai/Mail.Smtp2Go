namespace Scott.Mail.Smtp2Go.Tests.Integration.Subaccounts;

/// <summary>subaccounts/search against the sandbox. Skipped without a sandbox key; see <see cref="SandboxKey"/>. Add, edit, close and reopen are not exercised here.</summary>
[Trait("Category", "Sandbox")]
public class SandboxSubaccountTests
{
    [Fact]
    public async Task Search_returns_a_page_and_the_pager_walks_it()
    {
        Smtp2GoClient client = SandboxKey.CreateClient();
        CancellationToken ct = TestContext.Current.CancellationToken;

        ApiResponse<SubaccountSearchResult> page = await client.Subaccounts.SearchAsync(new SubaccountSearchRequest { PageSize = 10 }, cancellationToken: ct);
        List<Subaccount> all = [];
        await foreach (Subaccount subaccount in client.Subaccounts.SearchAllAsync(new SubaccountSearchRequest { PageSize = 10 }, cancellationToken: ct))
        {
            all.Add(subaccount);
        }

        page.RequestId.Should().NotBeNullOrWhiteSpace();
        page.Data.Subaccounts.Should().NotBeNull();
        page.Data.TotalCount.Should().NotBeNull();
        all.Should().HaveCount((int)page.Data.TotalCount!.Value);
        all.Should().OnlyContain(s => s.Id != null && s.State != SubaccountState.Unknown);
    }
}
