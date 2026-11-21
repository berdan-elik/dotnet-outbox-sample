namespace OutboxSample.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);
    public int BatchSize { get; set; } = 20;
    public int MaxAttempts { get; set; } = 5;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(5);
}
