using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OutboxSample.Application;
using OutboxSample.Application.Idempotency;
using OutboxSample.Application.Orders;
using OutboxSample.Infrastructure.Email;
using OutboxSample.Infrastructure.Outbox;
using OutboxSample.Infrastructure.Persistence;

namespace OutboxSample.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Read when the context is first built rather than here, so a host that swaps
        // configuration after registration (the test host does) is still honoured.
        services.AddDbContext<OrdersDbContext>((provider, options) => options
            .UseNpgsql(provider.GetRequiredService<IConfiguration>().GetConnectionString("Orders"))
            .UseSnakeCaseNamingConvention());
        services.AddScoped<IOrdersDbContext>(provider => provider.GetRequiredService<OrdersDbContext>());
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();

        services.AddSingleton<IEmailSender, LoggingEmailSender>();

        services.Configure<OutboxOptions>(configuration.GetSection("Outbox"));
        services.AddScoped<OutboxProcessor>();
        services.AddHostedService<OutboxDispatcher>();

        return services;
    }
}
