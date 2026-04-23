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
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.CuttingPlanId).IsUnique();
        builder.HasIndex(x => x.TenantId);
    }
}
