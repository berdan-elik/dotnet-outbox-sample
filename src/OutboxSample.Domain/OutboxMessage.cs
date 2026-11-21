namespace OutboxSample.Domain;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public Guid Id { get; private set; }
    public string Type { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public DateTimeOffset? DeadLetteredAt { get; private set; }

    public static OutboxMessage Create(string type, string payload, DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(now),
            Type = type,
            Payload = payload,
            CreatedAt = now,
            NextAttemptAt = now
        };

    public void MarkProcessed(DateTimeOffset now)
    {
        Attempts++;
        ProcessedAt = now;
        LastError = null;
    }

    public void ScheduleRetry(string error, DateTimeOffset nextAttemptAt)
    {
        Attempts++;
        LastError = error;
        NextAttemptAt = nextAttemptAt;
    }

    public void MarkDeadLettered(string error, DateTimeOffset now)
    {
        Attempts++;
        LastError = error;
        DeadLetteredAt = now;
    }
}
