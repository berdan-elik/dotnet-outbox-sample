using MediatR;
using OutboxSample.Application.Idempotency;

namespace OutboxSample.Application.Orders;

public sealed record CreateOrderCommand(string IdempotencyKey, string CustomerEmail, IReadOnlyList<CreateOrderLine> Lines)
    : IRequest<OrderResponse>, IIdempotentRequest;

public sealed record CreateOrderLine(string Sku, int Quantity, decimal UnitPrice);
