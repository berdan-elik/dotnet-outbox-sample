using MediatR;

namespace OutboxSample.Application.Orders;

public sealed record CreateOrderCommand(string CustomerEmail, IReadOnlyList<CreateOrderLine> Lines)
    : IRequest<OrderResponse>;

public sealed record CreateOrderLine(string Sku, int Quantity, decimal UnitPrice);
