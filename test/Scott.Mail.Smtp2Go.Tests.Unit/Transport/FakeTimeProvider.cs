namespace Scott.Mail.Smtp2Go.Tests.Unit.Transport;

/// <summary>A manual clock. Timers fire on the thread pool after advancing the clock by their due time, so <c>Task.Delay(.., provider)</c> completes without waiting and the requested delays are observable.</summary>
public sealed class FakeTimeProvider : TimeProvider
{
    private readonly object _gate = new();
    private long _ticks = TimeSpan.TicksPerSecond * 1_000_000;

    public List<TimeSpan> Delays { get; } = [];

    public TimeSpan Elapsed => TimeSpan.FromTicks(_ticks - (TimeSpan.TicksPerSecond * 1_000_000));

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp()
    {
        lock (_gate)
        {
            return _ticks;
        }
    }

    public override DateTimeOffset GetUtcNow()
    {
        return new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) + Elapsed;
    }

    public void Advance(TimeSpan by)
    {
        lock (_gate)
        {
            _ticks += by.Ticks;
        }
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        FakeTimer timer = new(this, callback, state);
        timer.Change(dueTime, period);
        return timer;
    }

    private sealed class FakeTimer(FakeTimeProvider owner, TimerCallback callback, object? state) : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            if (dueTime == Timeout.InfiniteTimeSpan)
            {
                return true;
            }

            lock (owner._gate)
            {
                owner.Delays.Add(dueTime);
            }

            owner.Advance(dueTime);
            _ = Task.Run(() => callback(state));
            return true;
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }
    }
}
