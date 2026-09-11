using Microsoft.Extensions.Options;
using Scott.Mail.Smtp2Go.Transport;

namespace Scott.Mail.Smtp2Go.DependencyInjection;

/// <summary>
/// Validates <see cref="Smtp2GoOptions"/> on start-up. Each message names the configuration key, for example
/// <c>Smtp2Go:ApiKey is required.</c> or <c>Smtp2Go:Marketing:Resilience:CircuitBreaker:FailureRatio must be greater than 0 and at most 1.</c>
/// </summary>
public sealed class Smtp2GoOptionsValidator : IValidateOptions<Smtp2GoOptions>
{
    private static readonly TimeSpan s_minimumCircuitDuration = TimeSpan.FromMilliseconds(500);
    private readonly Smtp2GoConfigurationPaths _paths;

    internal Smtp2GoOptionsValidator(Smtp2GoConfigurationPaths paths)
    {
        _paths = paths;
    }

    /// <summary>Creates a validator whose messages are prefixed with <c>Smtp2Go</c> (or <c>Smtp2Go:&lt;name&gt;</c> for named options).</summary>
    public Smtp2GoOptionsValidator()
        : this(new Smtp2GoConfigurationPaths())
    {
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, Smtp2GoOptions options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail("Smtp2GoOptions is null.");
        }

        string path = _paths.Get(name);
        List<string> failures = [];

        // The core messages start with the property name ("ApiKey is required."), so prefixing the section path yields the configuration key.
        foreach (string error in options.ToClientOptions().GetValidationErrors())
        {
            failures.Add(path + ":" + error);
        }

        ValidateResilience(path + ":" + nameof(Smtp2GoOptions.Resilience), options.Resilience, failures);

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateResilience(string path, ResilienceOptions resilience, List<string> failures)
    {
        if (resilience.MaxRetries < 0)
        {
            failures.Add($"{path}:{nameof(ResilienceOptions.MaxRetries)} must be 0 or greater.");
        }

        RequirePositive(path, nameof(ResilienceOptions.RetryBaseDelay), resilience.RetryBaseDelay, failures);
        RequirePositive(path, nameof(ResilienceOptions.AttemptTimeout), resilience.AttemptTimeout, failures);
        RequirePositive(path, nameof(ResilienceOptions.TotalTimeout), resilience.TotalTimeout, failures);
        if (resilience.AttemptTimeout > TimeSpan.Zero && resilience.TotalTimeout > TimeSpan.Zero && resilience.AttemptTimeout > resilience.TotalTimeout)
        {
            failures.Add($"{path}:{nameof(ResilienceOptions.AttemptTimeout)} must not exceed {path}:{nameof(ResilienceOptions.TotalTimeout)}.");
        }

        ValidateCircuitBreaker(path + ":" + nameof(ResilienceOptions.CircuitBreaker), resilience.CircuitBreaker, failures);
        ValidateRateLimiting(path + ":" + nameof(ResilienceOptions.RateLimiting), resilience.RateLimiting, failures);
    }

    private static void ValidateCircuitBreaker(string path, CircuitBreakerOptions breaker, List<string> failures)
    {
        if (!breaker.Enabled)
        {
            return;
        }

        if (breaker.FailureRatio <= 0 || breaker.FailureRatio > 1 || double.IsNaN(breaker.FailureRatio))
        {
            failures.Add($"{path}:{nameof(CircuitBreakerOptions.FailureRatio)} must be greater than 0 and at most 1.");
        }

        if (breaker.MinimumThroughput < 2)
        {
            failures.Add($"{path}:{nameof(CircuitBreakerOptions.MinimumThroughput)} must be at least 2.");
        }

        if (breaker.SamplingDuration < s_minimumCircuitDuration)
        {
            failures.Add($"{path}:{nameof(CircuitBreakerOptions.SamplingDuration)} must be at least 500 milliseconds.");
        }

        if (breaker.BreakDuration < s_minimumCircuitDuration)
        {
            failures.Add($"{path}:{nameof(CircuitBreakerOptions.BreakDuration)} must be at least 500 milliseconds.");
        }
    }

    private static void ValidateRateLimiting(string path, RateLimitingOptions limiting, List<string> failures)
    {
        if (!limiting.Enabled)
        {
            return;
        }

        if (limiting.GlobalConcurrency < 1)
        {
            failures.Add($"{path}:{nameof(RateLimitingOptions.GlobalConcurrency)} must be at least 1.");
        }

        if (limiting.GlobalQueueLimit < 0)
        {
            failures.Add($"{path}:{nameof(RateLimitingOptions.GlobalQueueLimit)} must be 0 or greater.");
        }

        foreach (KeyValuePair<RateLimitClass, RateLimitWindow> entry in limiting.Overrides)
        {
            string entryPath = $"{path}:{nameof(RateLimitingOptions.Overrides)}:{entry.Key}";
            if (entry.Key == RateLimitClass.None)
            {
                failures.Add($"{entryPath} cannot override {nameof(RateLimitClass.None)}; only documented classes are limited.");
            }

            if (entry.Value is null)
            {
                failures.Add($"{entryPath} must define {nameof(RateLimitWindow.PermitLimit)} and {nameof(RateLimitWindow.Window)}.");
                continue;
            }

            if (entry.Value.PermitLimit < 1)
            {
                failures.Add($"{entryPath}:{nameof(RateLimitWindow.PermitLimit)} must be at least 1.");
            }

            RequirePositive(entryPath, nameof(RateLimitWindow.Window), entry.Value.Window, failures);
            if (entry.Value.QueueLimit < 0)
            {
                failures.Add($"{entryPath}:{nameof(RateLimitWindow.QueueLimit)} must be 0 or greater.");
            }
        }
    }

    private static void RequirePositive(string path, string property, TimeSpan value, List<string> failures)
    {
        if (value <= TimeSpan.Zero)
        {
            failures.Add($"{path}:{property} must be a positive duration.");
        }
    }
}
