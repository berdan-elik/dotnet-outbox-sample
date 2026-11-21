using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OutboxSample.Application.Orders;
using OutboxSample.Infrastructure.Outbox;
using OutboxSample.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace OutboxSample.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public FakeEmailSender EmailSender { get; } = new();

    public async ValueTask InitializeAsync() => await _database.StartAsync();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Orders", _database.GetConnectionString());

        // No waiting between retries, and a low limit, so the tests can walk a
        // message through retry and dead-letter without sleeping.
        builder.UseSetting("Outbox:BaseDelay", "00:00:00");
        builder.UseSetting("Outbox:MaxAttempts", "3");

        builder.ConfigureTestServices(services =>
        {
            // Tests call OutboxProcessor themselves; the background loop would race them.
            services.Remove(services.Single(service => service.ImplementationType == typeof(OutboxDispatcher)));

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(EmailSender);
        });
    }

    public async Task ResetAsync()
    {
        EmailSender.Reset();

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await db.Database.ExecuteSqlAsync(
            $"TRUNCATE orders, order_lines, outbox_messages, idempotency_records");
    }

    public async Task<HttpResponseMessage> PostOrderAsync(string? idempotencyKey, string sku = "SKU-1", int quantity = 2)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/orders");
        request.Content = JsonContent.Create(new
        {
            customerEmail = "ada@example.com",
            lines = new[] { new { sku, quantity, unitPrice = 19.90m } }
        });

        if (idempotencyKey is not null)
            request.Headers.Add("Idempotency-Key", idempotencyKey);

        return await CreateClient().SendAsync(request, TestContext.Current.CancellationToken);
    }

    public async Task<int> ProcessOutboxBatchAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();
        return await processor.ProcessBatchAsync(TestContext.Current.CancellationToken);
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
