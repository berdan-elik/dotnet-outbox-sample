using OutboxSample.Domain;

namespace OutboxSample.Application.Orders;

public sealed record OrderResponse(
    Guid Id,
    string CustomerEmail,
    IReadOnlyList<OrderLineResponse> Lines,
    decimal Total,
    DateTimeOffset CreatedAt)
{
    public static OrderResponse From(Order order) =>
        new(
            order.Id,
            order.CustomerEmail,
            order.Lines.Select(line => new OrderLineResponse(line.Sku, line.Quantity, line.UnitPrice)).ToList(),
            order.Total,
            order.CreatedAt);
}

public sealed record OrderLineResponse(string Sku, int Quantity, decimal UnitPrice);
