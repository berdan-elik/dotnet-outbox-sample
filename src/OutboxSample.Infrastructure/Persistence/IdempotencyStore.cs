using Microsoft.EntityFrameworkCore;
using OutboxSample.Application.Idempotency;
using OutboxSample.Domain;

namespace OutboxSample.Infrastructure.Persistence;

internal sealed class IdempotencyStore(OrdersDbContext db) : IIdempotencyStore
{
    public Task<IdempotencyRecord?> FindAsync(string key, CancellationToken cancellationToken) =>
        db.IdempotencyRecords.AsNoTracking().FirstOrDefaultAsync(record => record.Key == key, cancellationToken);

    public async Task<bool> TryAddAsync(IdempotencyRecord record, CancellationToken cancellationToken)
    {
        // ON CONFLICT DO NOTHING instead of a plain insert: a duplicate key is an
        // expected outcome here, and a unique violation would abort the transaction.
        // If another transaction holds the key uncommitted, this waits for it.
        var inserted = await db.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO idempotency_records (key, request_hash, response_body, created_at)
             VALUES ({record.Key}, {record.RequestHash}, {record.ResponseBody}::jsonb, {record.CreatedAt})
             ON CONFLICT (key) DO NOTHING
             """,
            cancellationToken);

        return inserted == 1;
    }
}
