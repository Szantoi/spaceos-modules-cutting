#pragma warning disable CS0618 // Phase 3 stub — superseded by Execution.Domain in Phase 4
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Domain.Aggregates;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations;

public class CuttingExecutionConfiguration : IEntityTypeConfiguration<CuttingExecution>
{
    public void Configure(EntityTypeBuilder<CuttingExecution> builder)
    {
        builder.ToTable("CuttingExecutions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.CuttingSheetId).IsRequired();
        builder.Property(x => x.AssignedTo).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.WasteAreaCm2).HasPrecision(12, 4);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.CuttingSheetId).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.CompletedAt });
    }
}
