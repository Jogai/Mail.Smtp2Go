using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Scott.Mail.Smtp2Go.AspNetCore;

/// <summary>
/// Resolves the A and AAAA records of <see cref="HostName"/> (<c>webhooks.smtp2go.com</c>, the documented callback source) and caches them for
/// <see cref="CacheDuration"/> on the given <see cref="TimeProvider"/>. Concurrent callers share one lookup. When a refresh fails and an earlier result exists,
/// the earlier result is served and the next refresh is attempted a minute later; without one the exception propagates.
/// Override <see cref="LookupAsync"/> to replace DNS (tests), or register another <see cref="ISmtp2GoSourceIpResolver"/> altogether.
/// </summary>
public class Smtp2GoSourceIpResolver : ISmtp2GoSourceIpResolver
{
    /// <summary>The host whose addresses are allowed: <c>webhooks.smtp2go.com</c>.</summary>
    public const string HostName = "webhooks.smtp2go.com";

    /// <summary>How long a successful lookup is reused: one hour.</summary>
    public static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    /// <summary>How long a stale result is reused after a failed refresh before DNS is tried again: one minute.</summary>
    public static readonly TimeSpan RetryAfterFailure = TimeSpan.FromMinutes(1);

    private readonly TimeProvider _timeProvider;
    private readonly ILogger _logger;
    private readonly object _gate = new();
    private IPAddress[]? _addresses;
    private DateTimeOffset _expires;
    private Task<IPAddress[]>? _refresh;

    /// <summary>Creates a resolver.</summary>
    /// <param name="timeProvider">The clock for the cache; <see langword="null"/> for <see cref="TimeProvider.System"/>.</param>
    /// <param name="loggerFactory">Where to log lookups and failures; <see langword="null"/> for no logging.</param>
    public Smtp2GoSourceIpResolver(TimeProvider? timeProvider = null, ILoggerFactory? loggerFactory = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger(Smtp2GoWebhookEventIds.CategoryName);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyCollection<IPAddress>> GetAddressesAsync(CancellationToken cancellationToken)
    {
        Task<IPAddress[]> refresh;
        lock (_gate)
        {
            if (_addresses is not null && _timeProvider.GetUtcNow() < _expires)
            {
                return _addresses;
            }

            if (_refresh is null || _refresh.IsCompleted)
            {
                _refresh = RefreshAsync();
            }

            refresh = _refresh;
        }

        return await refresh.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Performs the lookup; the default is <see cref="Dns.GetHostAddressesAsync(string, CancellationToken)"/>. An empty result counts as a failure.</summary>
    protected virtual Task<IPAddress[]> LookupAsync(string hostName, CancellationToken cancellationToken)
    {
        return Dns.GetHostAddressesAsync(hostName, cancellationToken);
    }

    private async Task<IPAddress[]> RefreshAsync()
    {
        try
        {
            // The lookup is shared by every waiting request, so no single request's abort cancels it.
            IPAddress[] resolved = Normalize(await LookupAsync(HostName, CancellationToken.None).ConfigureAwait(false));
            if (resolved.Length == 0)
            {
                throw new InvalidOperationException($"DNS returned no addresses for {HostName}.");
            }

            DateTimeOffset expires = _timeProvider.GetUtcNow() + CacheDuration;
            lock (_gate)
            {
                _addresses = resolved;
                _expires = expires;
            }

            Smtp2GoWebhookLog.SourceIpResolved(_logger, HostName, resolved.Length, expires);
            return resolved;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            IPAddress[]? stale;
            lock (_gate)
            {
                stale = _addresses;
                if (stale is not null)
                {
                    _expires = _timeProvider.GetUtcNow() + RetryAfterFailure;
                }
            }

            if (stale is null)
            {
                throw;
            }

            Smtp2GoWebhookLog.SourceIpStaleCacheUsed(_logger, exception, HostName, stale.Length);
            return stale;
        }
    }

    private static IPAddress[] Normalize(IPAddress[] addresses)
    {
        IPAddress[] normalized = new IPAddress[addresses.Length];
        for (int i = 0; i < addresses.Length; i++)
        {
            normalized[i] = addresses[i].IsIPv4MappedToIPv6 ? addresses[i].MapToIPv4() : addresses[i];
        }

        return normalized;
    }
}
