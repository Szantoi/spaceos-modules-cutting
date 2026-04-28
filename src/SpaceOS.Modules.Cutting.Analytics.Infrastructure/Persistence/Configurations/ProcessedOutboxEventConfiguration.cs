using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Analytics.Domain.Common;

namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="ProcessedOutboxEvent"/>.
/// Uses composite PK on (EventId, SubscriberName) for atomic dedup INSERT.
/// No RLS — this is a cross-tenant dedup ledger keyed only on EventId + SubscriberName.
/// </summary>
public sealed class ProcessedOutboxEventConfiguration : IEntityTypeConfiguration<ProcessedOutboxEvent>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ProcessedOutboxEvent> builder)
    {
        builder.ToTable("ProcessedOutboxEvents", "cutting_analytics");
        builder.HasKey(e => new { e.EventId, e.SubscriberName });
        builder.Property(e => e.SubscriberName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        // Index used by the retention worker for time-based cleanup.
        builder.HasIndex(e => e.CreatedAt);
    }
}
