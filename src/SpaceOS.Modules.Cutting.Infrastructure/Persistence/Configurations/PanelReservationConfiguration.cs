using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Domain.Aggregates;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations;

public class PanelReservationConfiguration : IEntityTypeConfiguration<PanelReservation>
{
    public void Configure(EntityTypeBuilder<PanelReservation> builder)
    {
        builder.ToTable("PanelReservations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.CuttingPlanId).IsRequired();
        builder.Property(x => x.DaySlotId).IsRequired();
        builder.Property(x => x.InventoryReservationId).IsRequired();
        builder.Property(x => x.MaterialCode).IsRequired().HasMaxLength(200);
        builder.Property(x => x.WidthMm).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.HeightMm).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.CuttingPlanId);
        builder.HasIndex(x => x.DaySlotId);
    }
}
