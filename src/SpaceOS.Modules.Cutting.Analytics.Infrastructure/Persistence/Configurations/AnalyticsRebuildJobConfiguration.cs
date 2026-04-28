using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Analytics.Domain.Aggregates;

namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="AnalyticsRebuildJob"/>.</summary>
public sealed class AnalyticsRebuildJobConfiguration : IEntityTypeConfiguration<AnalyticsRebuildJob>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<AnalyticsRebuildJob> builder)
    {
        builder.ToTable("AnalyticsRebuildJobs", "cutting_analytics");
        builder.HasKey(j => j.Id);
        builder.Property(j => j.TenantId).IsRequired();
        builder.Property(j => j.Status).IsRequired().HasConversion<int>();
        builder.Property(j => j.RequestedAt).IsRequired();
        builder.Property(j => j.ErrorMessage).HasMaxLength(2048);
        builder.Property(j => j.ProcessedChunks).IsRequired();
        builder.Property(j => j.TotalChunks).IsRequired();

        builder.HasIndex(j => new { j.TenantId, j.Status });
    }
}
