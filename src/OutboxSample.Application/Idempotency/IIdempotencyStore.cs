using OutboxSample.Domain;

namespace OutboxSample.Application.Idempotency;

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> FindAsync(string key, CancellationToken cancellationToken);

    /// <summary>Inserts the record unless the key is already taken. Returns false if it was.</summary>
    Task<bool> TryAddAsync(IdempotencyRecord record, CancellationToken cancellationToken);
}
