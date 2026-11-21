using MediatR;
using Microsoft.AspNetCore.Mvc;
using OutboxSample.Application.Orders;

namespace OutboxSample.Api;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", async (
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            CreateOrderRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateOrderCommand(idempotencyKey ?? "", request.CustomerEmail, request.Lines);
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

public sealed record CreateOrderRequest(string CustomerEmail, IReadOnlyList<CreateOrderLine> Lines);
