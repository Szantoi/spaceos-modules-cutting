using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;

namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="DailyOperatorMetric"/>.</summary>
public sealed class DailyOperatorMetricConfiguration : IEntityTypeConfiguration<DailyOperatorMetric>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DailyOperatorMetric> builder)
    {
        builder.ToTable("DailyOperatorMetrics", "cutting_analytics");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.TenantId).IsRequired();
        builder.Property(m => m.WorkerId);  // nullable — null when suppressed (SEC-06)
        builder.Property(m => m.MetricDate).IsRequired();
        builder.Property(m => m.CompletedExecutions).IsRequired();
        builder.Property(m => m.AvgDurationMinutes).HasPrecision(8, 2).IsRequired();
        builder.Property(m => m.IsSuppressed).IsRequired();
        builder.Property(m => m.LastUpdatedAt).IsRequired();

        builder.HasIndex(m => new { m.TenantId, m.MetricDate });
        builder.HasIndex(m => m.TenantId);
    }
}
