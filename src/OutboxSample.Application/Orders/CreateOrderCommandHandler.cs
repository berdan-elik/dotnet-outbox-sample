using System.Text.Json;
using MediatR;
using OutboxSample.Domain;

namespace OutboxSample.Application.Orders;

public sealed class CreateOrderCommandHandler(IOrdersDbContext db, TimeProvider clock)
    : IRequestHandler<CreateOrderCommand, OrderResponse>
{
    public async Task<OrderResponse> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        var order = Order.Create(
            command.CustomerEmail,
            command.Lines.Select(line => new OrderLine(line.Sku, line.Quantity, line.UnitPrice)),
            now);

        var payload = JsonSerializer.Serialize(new OrderPlaced(order.Id, order.CustomerEmail, order.Total));

        db.Orders.Add(order);
        db.OutboxMessages.Add(OutboxMessage.Create(OrderPlaced.MessageType, payload, now));

        // The order and its outbox message go out in the same SaveChanges, so they are
        // written together or not at all. The surrounding transaction is opened and
        // committed by IdempotencyBehavior, together with the idempotency record.
        await db.SaveChangesAsync(cancellationToken);

        return OrderResponse.From(order);
    }
}
