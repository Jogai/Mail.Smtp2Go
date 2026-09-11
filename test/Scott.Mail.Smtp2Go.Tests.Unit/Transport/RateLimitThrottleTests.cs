using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Transport;

public class RateLimitThrottleTests
{
    [Fact]
    public async Task Sixty_calls_within_a_minute_pass_and_the_sixty_first_waits_for_a_token()
    {
        FakeTimeProvider clock = new();
        RateLimitThrottle throttle = new(60, TimeSpan.FromMinutes(1), clock);

        for (int i = 0; i < 60; i++)
        {
            await throttle.WaitAsync(TestContext.Current.CancellationToken);
        }

        clock.Delays.Should().BeEmpty(because: "the bucket starts full");

        await throttle.WaitAsync(TestContext.Current.CancellationToken);

        clock.Delays.Should().ContainSingle().Which.Should().BeCloseTo(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(10), because: "one token accrues per second at 60 per minute");
    }

    [Fact]
    public async Task Tokens_refill_with_elapsed_time_up_to_the_capacity()
    {
        FakeTimeProvider clock = new();
        RateLimitThrottle throttle = new(60, TimeSpan.FromMinutes(1), clock);
        for (int i = 0; i < 60; i++)
        {
            await throttle.WaitAsync(TestContext.Current.CancellationToken);
        }

        clock.Advance(TimeSpan.FromSeconds(30));
        for (int i = 0; i < 30; i++)
        {
            await throttle.WaitAsync(TestContext.Current.CancellationToken);
        }

        clock.Delays.Should().BeEmpty(because: "30 seconds accrue 30 tokens");

        clock.Advance(TimeSpan.FromHours(1));
        for (int i = 0; i < 60; i++)
        {
            await throttle.WaitAsync(TestContext.Current.CancellationToken);
        }

        clock.Delays.Should().BeEmpty(because: "an hour refills to the capacity, not beyond");
        await throttle.WaitAsync(TestContext.Current.CancellationToken);
        clock.Delays.Should().HaveCount(1);
    }

    [Fact]
    public async Task Waiting_honours_cancellation()
    {
        FakeTimeProvider clock = new();
        RateLimitThrottle throttle = new(1, TimeSpan.FromMinutes(1), clock);
        await throttle.WaitAsync(TestContext.Current.CancellationToken);
        using CancellationTokenSource cts = new();
        cts.Cancel();

        Func<Task> act = () => throttle.WaitAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Only_documented_classes_get_a_throttle()
    {
        RateLimitThrottle.Create(RateLimitClass.None).Should().BeNull();
        RateLimitThrottle.Create(RateLimitClass.ActivitySearch).Should().NotBeNull();
        RateLimitThrottle.Create(RateLimitClass.EmailSearch).Should().NotBeNull();
        RateLimitThrottle.Create(RateLimitClass.ApiKeyAdd).Should().NotBeNull();
        RateLimitThrottle.Create(RateLimitClass.SubaccountAdd).Should().NotBeNull();
    }

    [Fact]
    public void Invalid_arguments_are_rejected()
    {
        ((Func<RateLimitThrottle>)(() => new RateLimitThrottle(0, TimeSpan.FromMinutes(1)))).Should().Throw<ArgumentOutOfRangeException>();
        ((Func<RateLimitThrottle>)(() => new RateLimitThrottle(1, TimeSpan.Zero))).Should().Throw<ArgumentOutOfRangeException>();
    }
}
