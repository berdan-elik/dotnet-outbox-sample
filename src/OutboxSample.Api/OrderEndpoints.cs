using MediatR;
using OutboxSample.Application.Orders;

namespace OutboxSample.Api;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", async (CreateOrderCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var order = await sender.Send(command, cancellationToken);
            return Results.Created($"/orders/{order.Id}", order);
        });

        app.MapGet("/orders/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            var order = await sender.Send(new GetOrderQuery(id), cancellationToken);
            return order is null ? Results.NotFound() : Results.Ok(order);
        });
    }
}
