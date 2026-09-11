namespace Scott.Mail.Smtp2Go.DependencyInjection;

/// <summary>A sliding window of <see cref="PermitLimit"/> requests per <see cref="Window"/>, with up to <see cref="QueueLimit"/> requests waiting for the next slot.</summary>
public sealed class RateLimitWindow
{
    /// <summary>Requests allowed per <see cref="Window"/>. At least 1.</summary>
    public int PermitLimit { get; set; }

    /// <summary>Length of the window. Positive.</summary>
    public TimeSpan Window { get; set; }

    /// <summary>Requests that may wait for the next slot; the rest are rejected. Defaults to 1024.</summary>
    public int QueueLimit { get; set; } = 1024;
}
