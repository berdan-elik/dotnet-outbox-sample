namespace OutboxSample.Application.Idempotency;

public interface IIdempotentRequest
{
    string IdempotencyKey { get; }
}
