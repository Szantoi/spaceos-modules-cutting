using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Domain.Entities;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations;

public class CuttingBatchConfiguration : IEntityTypeConfiguration<CuttingBatch>
{
    public void Configure(EntityTypeBuilder<CuttingBatch> builder)
    {
        builder.ToTable("CuttingBatches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MaterialType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ThicknessMm).HasPrecision(5, 1);
        // SheetIds stored as CSV string
        builder.Property(x => x.SheetIds)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(Guid.Parse)
                      .ToList());
        builder.HasIndex(x => x.DailyCuttingPlanId);
    }
}
