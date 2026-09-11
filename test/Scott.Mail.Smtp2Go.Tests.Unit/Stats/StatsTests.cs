using System.Net;
using System.Text.Json;
using Scott.Mail.Smtp2Go.Json;
using Scott.Mail.Smtp2Go.Tests.Shared;
using Scott.Mail.Smtp2Go.Tests.Unit.Email;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Stats;

public class StatsTests
{
    private static readonly DateTimeOffset s_cycleStart = new(2022, 11, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset s_cycleEnd = new(2022, 11, 30, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Stats_family_is_seeded()
    {
        IEnumerable<Endpoint> stats = EndpointTable.All.Where(e => e.Path.StartsWith("stats/", StringComparison.Ordinal));

        stats.Select(e => e.Path).Should().BeEquivalentTo("stats/email_summary", "stats/email_cycle", "stats/email_bounces", "stats/email_spam", "stats/email_unsubs", "stats/email_history");
        stats.Should().OnlyContain(e => e.Idempotent && !e.AcceptsSubaccountId && e.RateLimit == RateLimitClass.None);
    }

    [Fact]
    public void Docs_summary_response_deserialises_with_an_empty_extra()
    {
        ApiResponse<EmailSummary> response = JsonSerializer.Deserialize(Fixture.Read("Stats/summary-response.json"), Smtp2GoJsonContext.Default.ApiResponseEmailSummary)!;

        EmailSummary data = response.Data;
        data.CycleStart.Should().Be(s_cycleStart);
        data.CycleEnd.Should().Be(s_cycleEnd);
        data.CycleUsed.Should().Be(1);
        data.CycleRemaining.Should().Be(9999);
        data.CycleMax.Should().Be(10000);
        data.EmailCount.Should().Be(1);
        data.Rejects.Should().Be(0);
#pragma warning disable CS0618
        data.BounceRejects.Should().Be(0);
        data.SpamRejects.Should().Be(0);
#pragma warning restore CS0618
        data.HardBounces.Should().Be(0);
        data.SoftBounces.Should().Be(0);
        data.BouncePercent.Should().Be(new Percentage(0m, "0.00"));
        data.SpamEmails.Should().Be(0);
        data.SpamPercent!.Value.Value.Should().Be(0);
        data.Unsubscribes.Should().Be(0);
        data.UnsubscribePercent.Should().Be(new Percentage(0m, "0.0"));
        data.Opens.Should().Be(0);
        data.Clicks.Should().Be(0);
        data.Extra.Should().BeNull();
    }

    [Fact]
    public void Docs_cycle_response_deserialises_with_an_empty_extra()
    {
        ApiResponse<EmailCycle> response = JsonSerializer.Deserialize(Fixture.Read("Stats/cycle-response.json"), Smtp2GoJsonContext.Default.ApiResponseEmailCycle)!;

        response.Data.Should().BeEquivalentTo(new EmailCycle { CycleStart = s_cycleStart, CycleEnd = s_cycleEnd, CycleUsed = 1, CycleRemaining = 9999, CycleMax = 10000 });
        response.Data.Extra.Should().BeNull();
    }

    [Fact]
    public void Docs_bounces_spam_and_unsubs_responses_deserialise_with_an_empty_extra()
    {
        ApiResponse<EmailBounces> bounces = JsonSerializer.Deserialize(Fixture.Read("Stats/bounces-response.json"), Smtp2GoJsonContext.Default.ApiResponseEmailBounces)!;
        ApiResponse<EmailSpam> spam = JsonSerializer.Deserialize(Fixture.Read("Stats/spam-response.json"), Smtp2GoJsonContext.Default.ApiResponseEmailSpam)!;
        ApiResponse<EmailUnsubscribes> unsubs = JsonSerializer.Deserialize(Fixture.Read("Stats/unsubs-response.json"), Smtp2GoJsonContext.Default.ApiResponseEmailUnsubscribes)!;

        bounces.Data.Should().BeEquivalentTo(new EmailBounces { Emails = 1, Rejects = 0, SoftBounces = 0, HardBounces = 0, BouncePercent = new Percentage(0m, "0.00") });
        spam.Data.Should().BeEquivalentTo(new EmailSpam { Emails = 1, Rejects = 0, Spams = 0, SpamPercent = new Percentage(0m, "0.00") });
        unsubs.Data.Should().BeEquivalentTo(new EmailUnsubscribes { Emails = 1, Rejects = 0, Unsubscribes = 0, UnsubscribePercent = new Percentage(0m, "0.00") });
        bounces.Data.Extra.Should().BeNull();
        spam.Data.Extra.Should().BeNull();
        unsubs.Data.Extra.Should().BeNull();
    }

    [Fact]
    public void Docs_history_response_deserialises_with_an_empty_extra()
    {
        ApiResponse<EmailHistory> response = JsonSerializer.Deserialize(Fixture.Read("Stats/history-response.json"), Smtp2GoJsonContext.Default.ApiResponseEmailHistory)!;

        response.Data.Count.Should().Be(1);
        EmailHistoryEntry row = response.Data.History.Should().ContainSingle().Which;
        row.EmailAddress.Should().Be("test3@example.com");
        row.Used.Should().Be(1);
        row.ByteCount.Should().Be(1022);
        row.AverageSize.Should().Be(1022);
        row.LastIp.Should().Be("82.1.149.48");
        row.Bounces.Should().Be(0);
        row.Extra.Should().BeNull();
        response.Data.Extra.Should().BeNull();
    }

    [Fact]
    public void Full_history_response_maps_every_schema_property_including_numeric_percentages()
    {
        ApiResponse<EmailHistory> response = JsonSerializer.Deserialize(Fixture.Read("Stats/history-response-full.json"), Smtp2GoJsonContext.Default.ApiResponseEmailHistory)!;

        EmailHistory data = response.Data;
        data.BouncePercentTotal!.Value.Value.Should().Be(1.24m);
        data.OpenPercentTotal!.Value.Value.Should().Be(12.5m);
        data.History.Should().HaveCount(3);
        EmailHistoryEntry domain = data.History![0];
        domain.Domain.Should().Be("test.com");
        domain.DomainVerified.Should().BeTrue();
        domain.AverageSize.Should().BeApproximately(1204.17, 0.01);
        domain.BouncePercent!.Value.Value.Should().Be(1.24m);
        domain.OpenPercent!.Value.Value.Should().Be(3.25m);
        domain.RejectPercent!.Value.Value.Should().Be(0m);
        domain.SpamPercent!.Value.Value.Should().Be(0.81m);
        domain.UnsubscribePercent!.Value.Value.Should().Be(0.81m);
        data.History[1].Username.Should().Be("my_user");
        data.History[1].Description.Should().Be("Marketing sends");
        data.History[2].Subaccount.Should().Be("My Subaccount Name");
        data.History.Should().OnlyContain(r => r.Extra == null);
        data.Extra.Should().BeNull();
    }

    [Fact]
    public void History_request_serialises_every_documented_field()
    {
        EmailHistoryRequest request = new()
        {
            GroupBy = EmailHistoryGroupBy.Subaccount,
            StartDate = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2026, 9, 1, 2, 0, 0, TimeSpan.FromHours(2)),
            Subaccounts = ["sub-1", "sub-2"],
        };

        Golden.AssertMatchesFixture(JsonSerializer.Serialize(request, Smtp2GoJsonContext.Default.EmailHistoryRequest), "Stats/history-request-full.json");
    }

    [Theory]
    [InlineData(EmailHistoryGroupBy.EmailAddress, "email_address")]
    [InlineData(EmailHistoryGroupBy.Username, "username")]
    [InlineData(EmailHistoryGroupBy.Domain, "domain")]
    [InlineData(EmailHistoryGroupBy.Subaccount, "subaccount")]
    public void Group_by_serialises_to_the_documented_names(EmailHistoryGroupBy groupBy, string wire)
    {
        JsonSerializer.Serialize(new EmailHistoryRequest { GroupBy = groupBy }, Smtp2GoJsonContext.Default.EmailHistoryRequest).Should().Be($$"""{"group_by":"{{wire}}"}""");
    }

    [Fact]
    public async Task Summary_bounces_spam_and_unsubs_send_only_the_username_filter()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler()
            .Respond("stats/email_summary", HttpStatusCode.OK, Fixture.Read("Stats/summary-response.json"))
            .Respond("stats/email_bounces", HttpStatusCode.OK, Fixture.Read("Stats/bounces-response.json"))
            .Respond("stats/email_spam", HttpStatusCode.OK, Fixture.Read("Stats/spam-response.json"))
            .Respond("stats/email_unsubs", HttpStatusCode.OK, Fixture.Read("Stats/unsubs-response.json"));
        Smtp2GoClient client = TestClient.Create(handler);

        await client.Stats.GetSummaryAsync("api-5BFDE1E62529");
        await client.Stats.GetBouncesAsync("api-5BFDE1E62529");
        await client.Stats.GetSpamAsync("api-5BFDE1E62529");
        await client.Stats.GetUnsubscribesAsync("api-5BFDE1E62529");
        await client.Stats.GetSummaryAsync();

        handler.Requests.Select(r => r.Endpoint!.Path).Should().Equal("stats/email_summary", "stats/email_bounces", "stats/email_spam", "stats/email_unsubs", "stats/email_summary");
        foreach (RecordedRequest request in handler.Requests.Take(4))
        {
            Golden.AssertMatchesFixture(request.Body!, "Stats/username-request.json");
        }

        handler.LastRequest.Body.Should().Be("{}", because: "no username means no filter, and no invented date range");
    }

    [Fact]
    public async Task Cycle_sends_an_empty_body_and_quota_reduces_it()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("stats/email_cycle", HttpStatusCode.OK, Fixture.Read("Stats/cycle-response.json"));
        Smtp2GoClient client = TestClient.Create(handler);

        ApiResponse<EmailCycle> cycle = await client.Stats.GetCycleAsync();
        Quota quota = await client.Stats.GetQuotaAsync();

        handler.Requests.Should().OnlyContain(r => r.Body == "{}" && r.Endpoint!.Path == "stats/email_cycle");
        cycle.Data.CycleMax.Should().Be(10000);
        quota.Should().Be(new Quota(1, 9999, 10000, s_cycleStart, s_cycleEnd, "4b84c952-9bca-432f-a68e-585e4c7a969c"));
        quota.UsedFraction.Should().BeApproximately(0.0001, 0.00001);
    }

    [Fact]
    public void Quota_fraction_is_zero_without_an_allowance_and_capped_at_one()
    {
        new Quota(5, 0, 0, null, null, "r").UsedFraction.Should().Be(0);
        new Quota(15, 0, 10, null, null, "r").UsedFraction.Should().Be(1);
    }

    [Fact]
    public async Task History_posts_the_request()
    {
        FakeHttpMessageHandler handler = new FakeHttpMessageHandler().Respond("stats/email_history", HttpStatusCode.OK, Fixture.Read("Stats/history-response.json"));
        Smtp2GoClient client = TestClient.Create(handler);

        ApiResponse<EmailHistory> response = await client.Stats.GetHistoryAsync(new EmailHistoryRequest { GroupBy = EmailHistoryGroupBy.Domain });

        handler.LastRequest.Endpoint!.Path.Should().Be("stats/email_history");
        handler.LastRequest.Body.Should().Be("""{"group_by":"domain"}""");
        response.Data.Count.Should().Be(1);
        await ((Func<Task>)(() => client.Stats.GetHistoryAsync(null!))).Should().ThrowAsync<ArgumentNullException>();
    }
}
