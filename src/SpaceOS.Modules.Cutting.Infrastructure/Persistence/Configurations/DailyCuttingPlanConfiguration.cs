using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Domain.Aggregates;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations;

public class DailyCuttingPlanConfiguration : IEntityTypeConfiguration<DailyCuttingPlan>
{
    public void Configure(EntityTypeBuilder<DailyCuttingPlan> builder)
    {
        builder.ToTable("DailyCuttingPlans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.PlanDate).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasMany(x => x.Batches).WithOne().HasForeignKey(b => b.DailyCuttingPlanId);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.PlanDate }).IsUnique();
    }
}
