using Microsoft.EntityFrameworkCore;
using OutboxSample.Infrastructure;
using OutboxSample.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Fine for a sample that runs as a single instance; a real deployment would run
// migrations as a separate step so that replicas do not race each other at startup.
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync();
}

app.MapGet("/health", () => Results.Ok());

app.Run();
