using System.Threading.RateLimiting;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.DependencyInjection;

/// <summary>
/// One sliding-window <see cref="RateLimiter"/> per documented <see cref="RateLimitClass"/> (overridable through <see cref="RateLimitingOptions.Overrides"/>)
/// plus one <see cref="ConcurrencyLimiter"/> for every request. One instance per registered client; disposed with its pipeline.
/// </summary>
internal sealed class EndpointRateLimiters : IDisposable
{
    private const int SegmentsPerWindow = 10;

    private readonly Dictionary<RateLimitClass, RateLimiter> _byClass = [];
    private readonly ConcurrencyLimiter _global;

    public EndpointRateLimiters(RateLimitingOptions options)
    {
        Argument.ThrowIfNull(options);
        _global = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
        {
            PermitLimit = options.GlobalConcurrency,
            QueueLimit = options.GlobalQueueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        });

        foreach (KeyValuePair<RateLimitClass, RateLimitWindow> documented in DocumentedWindows)
        {
            RateLimitWindow window = options.Overrides.TryGetValue(documented.Key, out RateLimitWindow? overridden) && overridden is not null ? overridden : documented.Value;
            _byClass[documented.Key] = CreateSlidingWindow(window);
        }

        foreach (KeyValuePair<RateLimitClass, RateLimitWindow> extra in options.Overrides)
        {
            if (extra.Key != RateLimitClass.None && extra.Value is not null && !_byClass.ContainsKey(extra.Key))
            {
                _byClass[extra.Key] = CreateSlidingWindow(extra.Value);
            }
        }
    }

    /// <summary>The limits SMTP2GO documents per class. <see cref="RateLimitClass.None"/> has no window.</summary>
    public static IReadOnlyDictionary<RateLimitClass, RateLimitWindow> DocumentedWindows { get; } = new Dictionary<RateLimitClass, RateLimitWindow>
    {
        [RateLimitClass.ActivitySearch] = new() { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) },
        [RateLimitClass.ApiKeyAdd] = new() { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) },
        [RateLimitClass.SubaccountAdd] = new() { PermitLimit = 50, Window = TimeSpan.FromHours(1) },
        [RateLimitClass.EmailSearch] = new() { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) },
    };

    /// <summary>The limiter for <paramref name="rateLimitClass"/>, or <see langword="null"/> for an unlimited class.</summary>
    public RateLimiter? GetLimiter(RateLimitClass rateLimitClass)
    {
        return _byClass.TryGetValue(rateLimitClass, out RateLimiter? limiter) ? limiter : null;
    }

    /// <summary>
    /// Acquires the class window (when the endpoint has one) and then a global concurrency permit. The returned lease releases both;
    /// when either could not be acquired the lease reports <see cref="RateLimitLease.IsAcquired"/> <see langword="false"/> and carries the limiter's retry-after metadata.
    /// </summary>
    public async ValueTask<RateLimitLease> AcquireAsync(Endpoint? endpoint, CancellationToken cancellationToken)
    {
        RateLimitLease? classLease = null;
        if (endpoint is not null && GetLimiter(endpoint.RateLimit) is { } limiter)
        {
            classLease = await limiter.AcquireAsync(1, cancellationToken).ConfigureAwait(false);
            if (!classLease.IsAcquired)
            {
                return classLease;
            }
        }

        RateLimitLease globalLease;
        try
        {
            globalLease = await _global.AcquireAsync(1, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            classLease?.Dispose();
            throw;
        }

        if (!globalLease.IsAcquired)
        {
            classLease?.Dispose();
            return globalLease;
        }

        return classLease is null ? globalLease : new CombinedLease(classLease, globalLease);
    }

    public void Dispose()
    {
        _global.Dispose();
        foreach (RateLimiter limiter in _byClass.Values)
        {
            limiter.Dispose();
        }
    }

    private static SlidingWindowRateLimiter CreateSlidingWindow(RateLimitWindow window)
    {
        return new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = window.PermitLimit,
            Window = window.Window,
            SegmentsPerWindow = SegmentsPerWindow,
            QueueLimit = window.QueueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true,
        });
    }

    /// <summary>A lease over a class permit and a concurrency permit; disposing it releases both.</summary>
    private sealed class CombinedLease : RateLimitLease
    {
        private readonly RateLimitLease _first;
        private readonly RateLimitLease _second;

        public CombinedLease(RateLimitLease first, RateLimitLease second)
        {
            _first = first;
            _second = second;
        }

        public override bool IsAcquired => _first.IsAcquired && _second.IsAcquired;

        public override IEnumerable<string> MetadataNames => _first.MetadataNames.Concat(_second.MetadataNames).Distinct(StringComparer.Ordinal);

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            return _first.TryGetMetadata(metadataName, out metadata) || _second.TryGetMetadata(metadataName, out metadata);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _second.Dispose();
                _first.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
