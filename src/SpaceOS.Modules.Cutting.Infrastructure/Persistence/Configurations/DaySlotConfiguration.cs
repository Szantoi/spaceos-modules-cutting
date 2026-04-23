using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Domain.Entities;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations;

public class DaySlotConfiguration : IEntityTypeConfiguration<DaySlot>
{
    public void Configure(EntityTypeBuilder<DaySlot> builder)
    {
        builder.ToTable("DaySlots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CuttingPlanId).IsRequired();
        builder.Property(x => x.SlotDate).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.CapacityHours).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.UsedCapacityHours).HasPrecision(8, 2).IsRequired();
        builder.HasMany(x => x.Jobs).WithOne().HasForeignKey(j => j.DaySlotId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.CuttingPlanId);
    }
}
