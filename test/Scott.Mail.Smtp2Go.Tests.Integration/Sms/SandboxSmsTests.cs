namespace Scott.Mail.Smtp2Go.Tests.Integration.Sms;

/// <summary>The SMS reporting endpoints against the sandbox. Skipped without a sandbox key; see <see cref="SandboxKey"/>. sms/send is not exercised (it costs money).</summary>
[Trait("Category", "Sandbox")]
public class SandboxSmsTests
{
    [Fact]
    public async Task Summary_and_sent_report_the_last_week()
    {
        Smtp2GoClient client = SandboxKey.CreateClient();
        CancellationToken ct = TestContext.Current.CancellationToken;
        DateTimeOffset start = DateTimeOffset.UtcNow.Date.AddDays(-7);

        ApiResponse<SmsSummary> summary = await client.Sms.GetSummaryAsync(new SmsSummaryRequest { StartDate = start }, cancellationToken: ct);
        ApiResponse<SmsSentResult> sent = await client.Sms.ViewSentAsync(new SmsSentRequest { StartDate = start }, cancellationToken: ct);

        summary.RequestId.Should().NotBeNullOrWhiteSpace();
        summary.Data.TotalMessages.Should().NotBeNull();
        sent.RequestId.Should().NotBeNullOrWhiteSpace();
        sent.Data.Messages.Should().NotBeNull();
    }
}
