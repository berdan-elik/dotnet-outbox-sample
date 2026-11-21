using Microsoft.EntityFrameworkCore;
using OutboxSample.Domain;

namespace OutboxSample.Application;

public interface IOrdersDbContext
{
    DbSet<Order> Orders { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<IdempotencyRecord> IdempotencyRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
