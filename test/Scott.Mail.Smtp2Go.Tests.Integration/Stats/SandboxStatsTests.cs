namespace Scott.Mail.Smtp2Go.Tests.Integration.Stats;

/// <summary>The six stats endpoints against the sandbox. Skipped without a sandbox key; see <see cref="SandboxKey"/>.</summary>
[Trait("Category", "Sandbox")]
public class SandboxStatsTests
{
    [Fact]
    public async Task All_six_endpoints_return_200()
    {
        Smtp2GoClient client = SandboxKey.CreateClient();
        CancellationToken ct = TestContext.Current.CancellationToken;

        ApiResponse<EmailSummary> summary = await client.Stats.GetSummaryAsync(cancellationToken: ct);
        ApiResponse<EmailCycle> cycle = await client.Stats.GetCycleAsync(cancellationToken: ct);
        ApiResponse<EmailBounces> bounces = await client.Stats.GetBouncesAsync(cancellationToken: ct);
        ApiResponse<EmailSpam> spam = await client.Stats.GetSpamAsync(cancellationToken: ct);
        ApiResponse<EmailUnsubscribes> unsubscribes = await client.Stats.GetUnsubscribesAsync(cancellationToken: ct);
        ApiResponse<EmailHistory> history = await client.Stats.GetHistoryAsync(new EmailHistoryRequest { GroupBy = EmailHistoryGroupBy.Username }, cancellationToken: ct);
        Quota quota = await client.Stats.GetQuotaAsync(cancellationToken: ct);

        summary.RequestId.Should().NotBeNullOrWhiteSpace();
        summary.Data.CycleMax.Should().NotBeNull();
        cycle.Data.CycleEnd.Should().NotBeNull();
        bounces.Data.Emails.Should().NotBeNull();
        spam.Data.Emails.Should().NotBeNull();
        unsubscribes.Data.Emails.Should().NotBeNull();
        history.Data.History.Should().NotBeNull();
        quota.Max.Should().Be(cycle.Data.CycleMax);
    }
}
