using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;

namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="DailyMaterialUsage"/>.</summary>
public sealed class DailyMaterialUsageConfiguration : IEntityTypeConfiguration<DailyMaterialUsage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DailyMaterialUsage> builder)
    {
        builder.ToTable("DailyMaterialUsages", "cutting_analytics");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.TenantId).IsRequired();
        builder.Property(m => m.MaterialCode).HasMaxLength(100).IsRequired();
        builder.Property(m => m.UsageDate).IsRequired();
        builder.Property(m => m.TotalAreaUsedMm2).HasPrecision(14, 2).IsRequired();
        builder.Property(m => m.WasteAreaMm2).HasPrecision(14, 2).IsRequired();
        builder.Property(m => m.OffcutCount).IsRequired();
        builder.Property(m => m.LastUpdatedAt).IsRequired();

        builder.HasIndex(m => new { m.TenantId, m.UsageDate, m.MaterialCode }).IsUnique();
        builder.HasIndex(m => m.TenantId);
    }
}
