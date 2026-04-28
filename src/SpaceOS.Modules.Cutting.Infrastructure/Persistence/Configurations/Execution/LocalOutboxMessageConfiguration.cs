using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Infrastructure.Outbox;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations.Execution;

/// <summary>EF Core configuration for <see cref="LocalOutboxMessage"/>.</summary>
public sealed class LocalOutboxMessageConfiguration : IEntityTypeConfiguration<LocalOutboxMessage>
{
    public void Configure(EntityTypeBuilder<LocalOutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages", "spaceos_cutting");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.BatchId);
        builder.Property(x => x.BatchSequenceNumber);
        builder.Property(x => x.AggregateId);
        builder.Property(x => x.AggregateType).HasMaxLength(128);
        builder.Property(x => x.EventType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.OccurredAt).IsRequired();
        builder.Property(x => x.ProcessedAt);
        builder.Property(x => x.Status).HasConversion<short>().IsRequired();
        builder.Property(x => x.Attempts).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(2048);

        // Partial index for pending messages — only index rows that need processing
        builder.HasIndex(x => new { x.Status, x.OccurredAt })
            .HasDatabaseName("IX_OutboxMessages_Status")
            .HasFilter("\"Status\" = 1");
    }
}
