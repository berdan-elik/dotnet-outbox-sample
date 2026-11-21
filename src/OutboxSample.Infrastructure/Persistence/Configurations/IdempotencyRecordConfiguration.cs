using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OutboxSample.Domain;

namespace OutboxSample.Infrastructure.Persistence.Configurations;

internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");
        builder.HasKey(record => record.Key);
        builder.Property(record => record.Key).HasMaxLength(100);
        builder.Property(record => record.RequestHash).HasMaxLength(64).IsFixedLength();
        builder.Property(record => record.ResponseBody).HasColumnType("jsonb");
    }
}
