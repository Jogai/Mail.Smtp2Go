namespace Scott.Mail.Smtp2Go.DependencyInjection;

/// <summary>Circuit-breaker settings. The breaker counts 5xx responses and transport failures only.</summary>
public sealed class CircuitBreakerOptions
{
    /// <summary>Whether the circuit breaker is part of the pipeline. Defaults to <see langword="true"/>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Failure ratio within <see cref="SamplingDuration"/> at which the circuit opens, in (0, 1]. Defaults to 0.5.</summary>
    public double FailureRatio { get; set; } = 0.5;

    /// <summary>Minimum number of calls within <see cref="SamplingDuration"/> before the ratio is evaluated (at least 2). Defaults to 20.</summary>
    public int MinimumThroughput { get; set; } = 20;

    /// <summary>Window over which failures are counted (at least 500 ms). Defaults to 30 seconds.</summary>
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>How long the circuit stays open before one probe call is let through (at least 500 ms). Defaults to 30 seconds.</summary>
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);
}
