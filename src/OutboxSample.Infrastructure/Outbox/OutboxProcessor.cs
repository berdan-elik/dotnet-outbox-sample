using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OutboxSample.Application.Orders;
using OutboxSample.Domain;
using OutboxSample.Infrastructure.Persistence;

namespace OutboxSample.Infrastructure.Outbox;

public sealed class OutboxProcessor(
    OrdersDbContext db,
    IEmailSender emailSender,
    TimeProvider clock,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor> logger)
{
    private readonly OutboxOptions _options = options.Value;

    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // SKIP LOCKED lets several dispatcher instances run side by side: each one
        // takes rows nobody else holds instead of waiting on, or re-sending, the same ones.
        // The row locks last until this transaction ends.
        var messages = await db.OutboxMessages
            .FromSql(
                $"""
                 SELECT * FROM outbox_messages
                 WHERE processed_at IS NULL AND dead_lettered_at IS NULL AND next_attempt_at <= {now}
                 ORDER BY next_attempt_at
                 LIMIT {_options.BatchSize}
                 FOR UPDATE SKIP LOCKED
                 """)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await DispatchAsync(message, cancellationToken);
                message.MarkProcessed(clock.GetUtcNow());
            }
            // A sender's own timeout also surfaces as OperationCanceledException, and that is
            // a failed attempt like any other. Only a shutdown is left to propagate.
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                RecordFailure(message, exception);
            }
        }

        // If the process dies before this commit, messages already sent stay pending
        // and are sent again on the next run: delivery is at-least-once.
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return messages.Count;
    }

    private Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken) =>
        message.Type switch
        {
            OrderPlaced.MessageType => emailSender.SendOrderConfirmationAsync(
                JsonSerializer.Deserialize<OrderPlaced>(message.Payload)!, cancellationToken),
            _ => throw new InvalidOperationException($"No handler for outbox message type '{message.Type}'.")
        };

    private void RecordFailure(OutboxMessage message, Exception exception)
    {
        var attempt = message.Attempts + 1;
        var now = clock.GetUtcNow();

        if (attempt >= _options.MaxAttempts)
        {
            message.MarkDeadLettered(exception.Message, now);
            logger.LogError(exception, "Outbox message {MessageId} dead-lettered after {Attempts} attempts",
                message.Id, attempt);
            return;
        }

        var delay = RetryBackoff.DelayAfter(attempt, _options.BaseDelay, _options.MaxDelay);
        message.ScheduleRetry(exception.Message, now + delay);
        logger.LogWarning(exception, "Outbox message {MessageId} failed on attempt {Attempt}, retrying in {Delay}",
            message.Id, attempt, delay);
    }
}
