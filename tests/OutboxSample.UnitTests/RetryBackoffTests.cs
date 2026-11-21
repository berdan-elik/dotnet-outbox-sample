using OutboxSample.Infrastructure.Outbox;
using Xunit;

namespace OutboxSample.UnitTests;

public class RetryBackoffTests
{
    private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromMinutes(5);

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 10)]
    [InlineData(3, 20)]
    [InlineData(4, 40)]
    public void Delay_doubles_with_each_failed_attempt(int attempt, int expectedSeconds)
    {
        var delay = RetryBackoff.DelayAfter(attempt, BaseDelay, MaxDelay);

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), delay);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(31)]
    [InlineData(1000)]
    public void Delay_never_exceeds_the_maximum(int attempt)
    {
        var delay = RetryBackoff.DelayAfter(attempt, BaseDelay, MaxDelay);

        Assert.Equal(MaxDelay, delay);
    }

    [Fact]
    public void Attempt_numbers_start_at_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryBackoff.DelayAfter(0, BaseDelay, MaxDelay));
    }
}
