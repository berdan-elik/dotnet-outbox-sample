namespace OutboxSample.Application.Idempotency;

public enum IdempotencyDecision
{
    Execute,
    Replay,
    Reject
}

public static class IdempotencyPolicy
{
    public static IdempotencyDecision Decide(string? storedRequestHash, string requestHash)
    {
        if (storedRequestHash is null)
            return IdempotencyDecision.Execute;

        return storedRequestHash == requestHash
            ? IdempotencyDecision.Replay
            : IdempotencyDecision.Reject;
    }
}
