using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations;

public class CuttingPlanConfiguration : IEntityTypeConfiguration<CuttingPlan>
{
    public void Configure(EntityTypeBuilder<CuttingPlan> builder)
    {
        builder.ToTable("CuttingPlans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.PlanDate).IsRequired();
        builder.Property(x => x.PlanDays).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.StrategyId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ProfileSnapshotId);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasMany(x => x.DaySlots).WithOne().HasForeignKey(d => d.CuttingPlanId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.PlanDate });
    }
}
