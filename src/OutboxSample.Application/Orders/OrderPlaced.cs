namespace OutboxSample.Application.Orders;

/// <summary>Payload of the outbox message written together with a new order.</summary>
public sealed record OrderPlaced(Guid OrderId, string CustomerEmail, decimal Total)
{
    public const string MessageType = "order.placed";
}
