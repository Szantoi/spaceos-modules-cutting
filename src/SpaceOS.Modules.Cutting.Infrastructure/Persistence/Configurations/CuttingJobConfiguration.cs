using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Domain.Aggregates;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations;

public class CuttingJobConfiguration : IEntityTypeConfiguration<CuttingJob>
{
    public void Configure(EntityTypeBuilder<CuttingJob> builder)
    {
        builder.ToTable("CuttingJobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DailyPlanId).IsRequired();
        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.ScheduledDate).IsRequired();
        builder.Property(x => x.Priority).IsRequired().HasMaxLength(20);
        builder.Property(x => x.EstimatedTimeHours).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasMaxLength(30);
        builder.HasIndex(x => x.DailyPlanId);
        builder.HasIndex(x => x.OrderId);
    }
}
