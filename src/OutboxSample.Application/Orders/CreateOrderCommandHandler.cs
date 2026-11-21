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

        // One SaveChanges is one database transaction: the order and its outbox
        // message are committed together or not at all.
        await db.SaveChangesAsync(cancellationToken);

        return OrderResponse.From(order);
    }
}
