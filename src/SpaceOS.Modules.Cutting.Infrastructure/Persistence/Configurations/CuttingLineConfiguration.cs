using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Domain.Entities;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations;

public class CuttingLineConfiguration : IEntityTypeConfiguration<CuttingLine>
{
    public void Configure(EntityTypeBuilder<CuttingLine> builder)
    {
        builder.ToTable("CuttingLines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CuttingSheetId).IsRequired();
        builder.Property(x => x.PartName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.MaterialType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.WidthMm).HasPrecision(10, 2);
        builder.Property(x => x.HeightMm).HasPrecision(10, 2);
        builder.Property(x => x.ThicknessMm).HasPrecision(5, 1);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => x.CuttingSheetId);
    }
}
