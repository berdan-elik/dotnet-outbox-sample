using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OutboxSample.Domain;

namespace OutboxSample.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();
        builder.Property(message => message.Type).HasMaxLength(200);
        builder.Property(message => message.Payload).HasColumnType("jsonb");

        // The dispatcher only ever looks for pending rows; a partial index keeps that
        // lookup small no matter how many processed rows pile up.
        builder.HasIndex(message => message.NextAttemptAt)
            .HasFilter("processed_at IS NULL AND dead_lettered_at IS NULL");
    }
}
