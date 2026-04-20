using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Domain.Aggregates;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations;

public class DailyPlanConfiguration : IEntityTypeConfiguration<DailyPlan>
{
    public void Configure(EntityTypeBuilder<DailyPlan> builder)
    {
        builder.ToTable("DailyPlans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CuttingPlanId).IsRequired();
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.AvailableCapacity).HasPrecision(8, 2).IsRequired();
        builder.HasMany(x => x.Jobs).WithOne().HasForeignKey(j => j.DailyPlanId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.CuttingPlanId);
    }
}
