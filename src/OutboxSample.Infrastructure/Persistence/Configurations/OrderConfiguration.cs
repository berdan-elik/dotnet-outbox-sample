using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OutboxSample.Domain;

namespace OutboxSample.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();
        builder.Property(order => order.CustomerEmail).HasMaxLength(320);

        builder.OwnsMany(order => order.Lines, lines =>
        {
            lines.ToTable("order_lines");
            lines.WithOwner().HasForeignKey("OrderId");
            lines.Property<int>("Id");
            lines.HasKey("Id");
            lines.Property(line => line.Sku).HasMaxLength(64);
            lines.Property(line => line.UnitPrice).HasPrecision(12, 2);
        });
    }
}
