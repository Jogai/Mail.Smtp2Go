namespace Scott.Mail.Smtp2Go.Transport;

/// <summary>
/// A minimal token bucket: <c>capacity</c> calls per <c>period</c>, refilled continuously. <see cref="WaitAsync"/> returns at once while tokens remain and otherwise
/// waits until the next token accrues. One instance per <see cref="RateLimitClass"/> per client; the dependency injection package replaces this with a Polly pipeline.
/// </summary>
internal sealed class RateLimitThrottle
{
    private readonly object _gate = new();
    private readonly double _capacity;
    private readonly double _tokensPerTick;
    private readonly TimeProvider _timeProvider;
    private double _tokens;
    private long _lastRefill;

    public RateLimitThrottle(int capacity, TimeSpan period, TimeProvider? timeProvider = null)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "The capacity must be positive.");
        }

        if (period <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(period), period, "The period must be positive.");
        }

        _timeProvider = timeProvider ?? TimeProvider.System;
        _capacity = capacity;
        _tokens = capacity;
        _tokensPerTick = capacity / (period.TotalSeconds * _timeProvider.TimestampFrequency);
        _lastRefill = _timeProvider.GetTimestamp();
    }

    /// <summary>The throttle for a documented class, or <see langword="null"/> when the class has no client-side limit.</summary>
    public static RateLimitThrottle? Create(RateLimitClass rateLimitClass, TimeProvider? timeProvider = null)
    {
        return rateLimitClass switch
        {
            RateLimitClass.ActivitySearch => new RateLimitThrottle(60, TimeSpan.FromMinutes(1), timeProvider),
            RateLimitClass.EmailSearch => new RateLimitThrottle(20, TimeSpan.FromMinutes(1), timeProvider),
            RateLimitClass.ApiKeyAdd => new RateLimitThrottle(5, TimeSpan.FromMinutes(1), timeProvider),
            RateLimitClass.SubaccountAdd => new RateLimitThrottle(50, TimeSpan.FromHours(1), timeProvider),
            _ => null,
        };
    }

    /// <summary>Takes one token, waiting for it to accrue when the bucket is empty.</summary>
    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            TimeSpan wait;
            lock (_gate)
            {
                Refill();
                if (_tokens >= 1)
                {
                    _tokens -= 1;
                    return;
                }

                double ticks = (1 - _tokens) / _tokensPerTick;
                wait = TimeSpan.FromSeconds(ticks / _timeProvider.TimestampFrequency);
            }

            if (wait < TimeSpan.FromMilliseconds(1))
            {
                wait = TimeSpan.FromMilliseconds(1);
            }

#if NET8_0_OR_GREATER
            await Task.Delay(wait, _timeProvider, cancellationToken).ConfigureAwait(false);
#else
            await _timeProvider.Delay(wait, cancellationToken).ConfigureAwait(false);
#endif
        }
    }

    private void Refill()
    {
        long now = _timeProvider.GetTimestamp();
        long elapsed = now - _lastRefill;
        if (elapsed > 0)
        {
            _tokens = Math.Min(_capacity, _tokens + (elapsed * _tokensPerTick));
            _lastRefill = now;
        }
    }
}
