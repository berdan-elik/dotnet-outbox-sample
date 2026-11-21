namespace OutboxSample.Application.Idempotency;

public sealed class IdempotencyKeyReusedException(string key)
    : Exception($"Idempotency key '{key}' was already used with a different request.");
