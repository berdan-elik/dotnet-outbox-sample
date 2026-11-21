using System.Text.Json;
using MediatR;
using OutboxSample.Application.Idempotency;
using OutboxSample.Domain;

namespace OutboxSample.Application.Behaviors;

public sealed class IdempotencyBehavior<TRequest, TResponse>(
    IOrdersDbContext db, IIdempotencyStore store, TimeProvider clock)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IIdempotentRequest { IdempotencyKey: var key })
            return await next(cancellationToken);

        var requestHash = RequestHash.Compute(request);

        var stored = await store.FindAsync(key, cancellationToken);
        if (stored is not null)
            return Replay(stored, requestHash);

        // The handler's writes and the idempotency record share one transaction,
        // so a key is never recorded without its order or the other way around.
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);

        var response = await next(cancellationToken);
        var record = IdempotencyRecord.Create(key, requestHash, JsonSerializer.Serialize(response), clock.GetUtcNow());

        if (await store.TryAddAsync(record, cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return response;
        }

        // A concurrent request with the same key committed first. Roll back what
        // the handler wrote here and answer with what that request stored.
        await transaction.RollbackAsync(cancellationToken);

        stored = await store.FindAsync(key, cancellationToken)
            ?? throw new InvalidOperationException($"Idempotency record '{key}' disappeared after a conflict.");
        return Replay(stored, requestHash);
    }

    private static TResponse Replay(IdempotencyRecord stored, string requestHash) =>
        IdempotencyPolicy.Decide(stored.RequestHash, requestHash) == IdempotencyDecision.Replay
            ? JsonSerializer.Deserialize<TResponse>(stored.ResponseBody)!
            : throw new IdempotencyKeyReusedException(stored.Key);
}
