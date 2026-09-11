namespace Scott.Mail.Smtp2Go;

/// <summary>The operational view of <c>stats/email_cycle</c>: how much of the plan is used and when the cycle rolls over.</summary>
/// <param name="Used">Emails sent this cycle.</param>
/// <param name="Remaining">Emails left this cycle.</param>
/// <param name="Max">The cycle allowance.</param>
/// <param name="CycleStart">Start of the cycle (UTC).</param>
/// <param name="CycleEnd">End of the cycle (UTC).</param>
/// <param name="RequestId">The API request id the figures came from.</param>
public sealed record Quota(long Used, long Remaining, long Max, DateTimeOffset? CycleStart, DateTimeOffset? CycleEnd, string RequestId)
{
    /// <summary>The used share of the allowance, 0 to 1; 0 when <see cref="Max"/> is 0.</summary>
    public double UsedFraction => Max <= 0 ? 0 : Math.Min(1, (double)Used / Max);

    /// <summary>Creates the quota from an <c>email_cycle</c> response.</summary>
    public static Quota From(ApiResponse<EmailCycle> response)
    {
        Argument.ThrowIfNull(response);
        EmailCycle cycle = response.Data ?? new EmailCycle();
        return new Quota(cycle.CycleUsed ?? 0, cycle.CycleRemaining ?? 0, cycle.CycleMax ?? 0, cycle.CycleStart, cycle.CycleEnd, response.RequestId);
    }
}
