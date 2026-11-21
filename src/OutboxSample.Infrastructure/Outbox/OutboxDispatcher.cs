using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OutboxSample.Infrastructure.Outbox;

public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private readonly OutboxOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = 0;

            try
            {
                // The processor uses a scoped DbContext; a hosted service is a singleton,
                // so each batch gets its own scope.
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();
                processed = await processor.ProcessBatchAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Outbox batch failed");
            }

            // A full batch usually means more rows are waiting, so only pause when it was not full.
            if (processed < _options.BatchSize)
                await Task.Delay(_options.PollInterval, stoppingToken);
        }
    }
}
