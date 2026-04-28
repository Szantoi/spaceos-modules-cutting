using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations;

public class CuttingJobConfiguration : IEntityTypeConfiguration<CuttingJob>
{
    public void Configure(EntityTypeBuilder<CuttingJob> builder)
    {
        builder.ToTable("CuttingJobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DaySlotId).IsRequired();
        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.ScheduledDate).IsRequired();
        builder.Property(x => x.Priority).IsRequired().HasMaxLength(20);
        builder.Property(x => x.EstimatedTimeHours).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasMaxLength(30);
        builder.Property(x => x.WidthMm).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.HeightMm).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.Material).HasMaxLength(100).HasDefaultValue("");
        builder.Property(x => x.GrainDirection).HasDefaultValue(GrainDirection.None);
        builder.HasIndex(x => x.DaySlotId);
        builder.HasIndex(x => x.OrderId);
    }
}
