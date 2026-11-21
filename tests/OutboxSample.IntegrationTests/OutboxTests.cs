using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OutboxSample.Domain;
using OutboxSample.Infrastructure.Persistence;
using Xunit;

namespace OutboxSample.IntegrationTests;

[Collection(ApiCollection.Name)]
public class OutboxTests(ApiFactory factory) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task A_message_is_sent_once_and_marked_processed()
    {
        await factory.PostOrderAsync(Guid.NewGuid().ToString());

        Assert.Equal(1, await factory.ProcessOutboxBatchAsync());
        Assert.Equal(0, await factory.ProcessOutboxBatchAsync());

        var message = await SingleMessageAsync();
        Assert.NotNull(message.ProcessedAt);
        Assert.Equal(1, message.Attempts);
        Assert.Single(factory.EmailSender.Sent);
    }

    [Fact]
    public async Task A_failed_send_keeps_the_message_and_it_is_retried()
    {
        await factory.PostOrderAsync(Guid.NewGuid().ToString());
        factory.EmailSender.OnSend = _ => throw new InvalidOperationException("provider is down");

        await factory.ProcessOutboxBatchAsync();

        var failed = await SingleMessageAsync();
        Assert.Null(failed.ProcessedAt);
        Assert.Null(failed.DeadLetteredAt);
        Assert.Equal(1, failed.Attempts);
        Assert.Equal("provider is down", failed.LastError);
        Assert.Empty(factory.EmailSender.Sent);

        factory.EmailSender.OnSend = null;
        await factory.ProcessOutboxBatchAsync();

        var retried = await SingleMessageAsync();
        Assert.NotNull(retried.ProcessedAt);
        Assert.Equal(2, retried.Attempts);
        Assert.Single(factory.EmailSender.Sent);
    }

    [Fact]
    public async Task A_message_that_keeps_failing_is_dead_lettered()
    {
        await factory.PostOrderAsync(Guid.NewGuid().ToString());
        factory.EmailSender.OnSend = _ => throw new InvalidOperationException("provider is down");

        // MaxAttempts is 3 in the test configuration.
        await factory.ProcessOutboxBatchAsync();
        await factory.ProcessOutboxBatchAsync();
        await factory.ProcessOutboxBatchAsync();

        var message = await SingleMessageAsync();
        Assert.NotNull(message.DeadLetteredAt);
        Assert.Null(message.ProcessedAt);
        Assert.Equal(3, message.Attempts);
        Assert.Equal(0, await factory.ProcessOutboxBatchAsync());
    }

    [Fact]
    public async Task A_second_processor_skips_rows_locked_by_the_first()
    {
        await factory.PostOrderAsync(Guid.NewGuid().ToString());

        var sendStarted = new TaskCompletionSource();
        var releaseSend = new TaskCompletionSource();
        factory.EmailSender.OnSend = async _ =>
        {
            sendStarted.SetResult();
            await releaseSend.Task;
        };

        // The first processor is stuck inside the send, holding the row lock.
        var first = factory.ProcessOutboxBatchAsync();
        await sendStarted.Task;

        Assert.Equal(0, await factory.ProcessOutboxBatchAsync());

        releaseSend.SetResult();
        Assert.Equal(1, await first);
        Assert.Single(factory.EmailSender.Sent);
    }

    private async Task<OutboxMessage> SingleMessageAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        return await db.OutboxMessages.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
    }
}
