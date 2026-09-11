namespace Scott.Mail.Smtp2Go.Tests.Integration.ApiKeys;

/// <summary>The read-only api_keys endpoints against the sandbox. Skipped without a sandbox key; see <see cref="SandboxKey"/>. Mutating calls are not exercised here.</summary>
[Trait("Category", "Sandbox")]
public class SandboxApiKeyTests
{
    [Fact]
    public async Task Permissions_lists_the_endpoints_the_key_may_call()
    {
        Smtp2GoClient client = SandboxKey.CreateClient();

        ApiResponse<IReadOnlyList<string>> permissions = await client.ApiKeys.GetPermissionsAsync(cancellationToken: TestContext.Current.CancellationToken);

        permissions.RequestId.Should().NotBeNullOrWhiteSpace();
        permissions.Data.Should().NotBeEmpty();
        permissions.Data.Should().OnlyContain(p => p.StartsWith('/') || p == "*");
    }

    [Fact]
    public async Task View_lists_the_keys_masked()
    {
        Smtp2GoClient client = SandboxKey.CreateClient();

        ApiResponse<IReadOnlyList<ApiKey>> keys = await client.ApiKeys.ViewAsync(new ApiKeyViewRequest(), cancellationToken: TestContext.Current.CancellationToken);

        keys.RequestId.Should().NotBeNullOrWhiteSpace();
        keys.Data.Should().NotBeNull();
        keys.Data.Should().OnlyContain(k => k.Key != null && k.Status != null);
    }
}
