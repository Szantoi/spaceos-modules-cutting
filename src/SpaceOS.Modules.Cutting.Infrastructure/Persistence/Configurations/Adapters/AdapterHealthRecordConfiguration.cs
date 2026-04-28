using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Domain.Adapters;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations.Adapters;

internal sealed class AdapterHealthRecordConfiguration : IEntityTypeConfiguration<AdapterHealthRecord>
{
    public void Configure(EntityTypeBuilder<AdapterHealthRecord> builder)
    {
        builder.ToTable("adapter_health_record", "spaceos_cutting");
        builder.HasKey(x => new { x.TenantId, x.AdapterName });

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.AdapterName)
            .HasColumnName("adapter_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.LastCheckAt)
            .HasColumnName("last_check_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.LastSuccessAt)
            .HasColumnName("last_success_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.IsHealthy)
            .HasColumnName("is_healthy")
            .IsRequired();

        builder.Property(x => x.LastErrorMessage)
            .HasColumnName("last_error_message")
            .HasMaxLength(8000);

        builder.Property(x => x.ConsecutiveFailures)
            .HasColumnName("consecutive_failures")
            .IsRequired();

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_ahr_tenant_id");

        builder.HasIndex(x => x.IsHealthy)
            .HasDatabaseName("ix_ahr_is_healthy");
    }
}
