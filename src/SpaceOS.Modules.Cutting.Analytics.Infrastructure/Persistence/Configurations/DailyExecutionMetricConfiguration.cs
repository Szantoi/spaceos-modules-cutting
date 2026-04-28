using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;

namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="DailyExecutionMetric"/>.</summary>
public sealed class DailyExecutionMetricConfiguration : IEntityTypeConfiguration<DailyExecutionMetric>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DailyExecutionMetric> builder)
    {
        builder.ToTable("DailyExecutionMetrics", "cutting_analytics");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.TenantId).IsRequired();
        builder.Property(m => m.MachineId).HasMaxLength(100).IsRequired();
        builder.Property(m => m.MetricDate).IsRequired();
        builder.Property(m => m.CompletedCount).IsRequired();
        builder.Property(m => m.AvgDurationMinutes).HasPrecision(8, 2).IsRequired();
        builder.Property(m => m.YieldPercent).HasPrecision(5, 2).IsRequired();
        builder.Property(m => m.LastUpdatedAt).IsRequired();

        builder.HasIndex(m => new { m.TenantId, m.MetricDate, m.MachineId }).IsUnique();
        builder.HasIndex(m => m.TenantId);
    }
}
