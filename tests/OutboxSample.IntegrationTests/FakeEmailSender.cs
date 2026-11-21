using System.Collections.Concurrent;
using OutboxSample.Application.Orders;

namespace OutboxSample.IntegrationTests;

public sealed class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<OrderPlaced> _sent = new();

    /// <summary>Runs before a send is recorded; throw here to simulate a provider failure.</summary>
    public Func<OrderPlaced, Task>? OnSend { get; set; }

    public IReadOnlyCollection<OrderPlaced> Sent => _sent;

    public async Task SendOrderConfirmationAsync(OrderPlaced order, CancellationToken cancellationToken)
    {
        if (OnSend is not null)
            await OnSend(order);

        _sent.Enqueue(order);
    }

    public void Reset()
    {
        OnSend = null;
        _sent.Clear();
    }
}
