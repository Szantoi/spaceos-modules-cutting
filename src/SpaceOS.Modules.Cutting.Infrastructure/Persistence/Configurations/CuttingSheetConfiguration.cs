using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Domain.Aggregates;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations;

public class CuttingSheetConfiguration : IEntityTypeConfiguration<CuttingSheet>
{
    public void Configure(EntityTypeBuilder<CuttingSheet> builder)
    {
        builder.ToTable("CuttingSheets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.OrderReference).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.CuttingSheetId);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}
