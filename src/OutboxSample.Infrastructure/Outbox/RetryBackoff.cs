namespace OutboxSample.Infrastructure.Outbox;

public static class RetryBackoff
{
    /// <summary>Delay before the next try after <paramref name="attempt"/> failed attempts: base, 2x, 4x, ... up to max.</summary>
    public static TimeSpan DelayAfter(int attempt, TimeSpan baseDelay, TimeSpan maxDelay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(attempt, 1);

        // Past this many doublings the result exceeds any sensible max and would overflow.
        if (attempt > 30)
            return maxDelay;

        var delay = baseDelay * Math.Pow(2, attempt - 1);
        return delay < maxDelay ? delay : maxDelay;
    }
}
