using MediatR;
using Microsoft.EntityFrameworkCore;

namespace OutboxSample.Application.Orders;

public sealed record GetOrderQuery(Guid Id) : IRequest<OrderResponse?>;

public sealed class GetOrderQueryHandler(IOrdersDbContext db) : IRequestHandler<GetOrderQuery, OrderResponse?>
{
    public async Task<OrderResponse?> Handle(GetOrderQuery query, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == query.Id, cancellationToken);

        return order is null ? null : OrderResponse.From(order);
    }
}
