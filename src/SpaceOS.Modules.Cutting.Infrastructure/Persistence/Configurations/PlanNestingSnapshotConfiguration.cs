using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Domain.Entities;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations;

internal sealed class PlanNestingSnapshotConfiguration : IEntityTypeConfiguration<PlanNestingSnapshot>
{
    public void Configure(EntityTypeBuilder<PlanNestingSnapshot> builder)
    {
        builder.ToTable("PlanNestingSnapshots", "spaceos_cutting");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CuttingPlanId).IsRequired();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.NestingResultJson).IsRequired().HasColumnType("jsonb");
        builder.Property(x => x.PlacementsJson).HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.YieldPercent).HasPrecision(8, 2).HasDefaultValue(0m);
        builder.Property(x => x.WasteAreaMm2).HasDefaultValue(0L);
        builder.Property(x => x.Algorithm).HasMaxLength(50).HasDefaultValue("");
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.CuttingPlanId).IsUnique();
        builder.HasIndex(x => x.TenantId);
    }
}
