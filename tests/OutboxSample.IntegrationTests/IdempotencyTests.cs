using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OutboxSample.Application.Orders;
using OutboxSample.Infrastructure.Persistence;
using Xunit;

namespace OutboxSample.IntegrationTests;

[Collection(ApiCollection.Name)]
public class IdempotencyTests(ApiFactory factory) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Concurrent_requests_with_the_same_key_create_one_order_and_one_outbox_message()
    {
        var key = Guid.NewGuid().ToString();

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => factory.PostOrderAsync(key)));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));

        var orders = await Task.WhenAll(responses.Select(ReadOrderAsync));
        Assert.Single(orders.Select(order => order.Id).Distinct());

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var cancellationToken = TestContext.Current.CancellationToken;

        Assert.Equal(1, await db.Orders.CountAsync(cancellationToken));
        Assert.Equal(1, await db.OutboxMessages.CountAsync(cancellationToken));
        Assert.Equal(1, await db.IdempotencyRecords.CountAsync(cancellationToken));
    }

    [Fact]
    public async Task Repeating_a_request_returns_the_first_response()
    {
        var key = Guid.NewGuid().ToString();

        var first = await ReadOrderAsync(await factory.PostOrderAsync(key));
        var secondResponse = await factory.PostOrderAsync(key);
        var second = await ReadOrderAsync(secondResponse);

        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.Total, second.Total);
        Assert.Equal(first.CreatedAt, second.CreatedAt);
    }

    [Fact]
    public async Task Reusing_a_key_with_a_different_body_is_rejected()
    {
        var key = Guid.NewGuid().ToString();
        await factory.PostOrderAsync(key, quantity: 2);

        var response = await factory.PostOrderAsync(key, quantity: 3);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_request_without_a_key_is_a_bad_request()
    {
        var response = await factory.PostOrderAsync(idempotencyKey: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_created_order_can_be_read_back()
    {
        var created = await ReadOrderAsync(await factory.PostOrderAsync(Guid.NewGuid().ToString()));

        var fetched = await factory.CreateClient().GetFromJsonAsync<OrderResponse>(
            $"/orders/{created.Id}", TestContext.Current.CancellationToken);

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal("ada@example.com", fetched.CustomerEmail);
        Assert.Equal(39.80m, fetched.Total);
    }

    private static async Task<OrderResponse> ReadOrderAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<OrderResponse>(TestContext.Current.CancellationToken))!;
}
