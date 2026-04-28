using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;

namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="MachineOEEHourly"/>. Maps <c>OEEScore</c> as an owned entity.</summary>
public sealed class MachineOEEHourlyConfiguration : IEntityTypeConfiguration<MachineOEEHourly>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<MachineOEEHourly> builder)
    {
        builder.ToTable("MachineOEEHourlies", "cutting_analytics");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.TenantId).IsRequired();
        builder.Property(m => m.MachineId).HasMaxLength(100).IsRequired();
        builder.Property(m => m.HourSlot).IsRequired();
        builder.Property(m => m.LastUpdatedAt).IsRequired();

        builder.OwnsOne(m => m.Score, scoreBuilder =>
        {
            scoreBuilder.Property(s => s.Availability)
                .HasPrecision(5, 4).IsRequired().HasColumnName("Availability");
            scoreBuilder.Property(s => s.Performance)
                .HasPrecision(5, 4).IsRequired().HasColumnName("Performance");
            scoreBuilder.Property(s => s.Quality)
                .HasPrecision(5, 4).IsRequired().HasColumnName("Quality");
        });

        builder.HasIndex(m => new { m.TenantId, m.MachineId, m.HourSlot }).IsUnique();
        builder.HasIndex(m => m.TenantId);
    }
}
