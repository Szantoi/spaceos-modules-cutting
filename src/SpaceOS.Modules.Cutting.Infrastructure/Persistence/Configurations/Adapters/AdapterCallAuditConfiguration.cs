using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations.Adapters;

internal sealed class AdapterCallAuditConfiguration : IEntityTypeConfiguration<AdapterCallAuditEntity>
{
    public void Configure(EntityTypeBuilder<AdapterCallAuditEntity> builder)
    {
        // NOTE: The actual DDL creates this as a PARTITIONED table by range on started_at.
        // EF Core does not model partitions natively — the partition structure is managed via migrations.
        builder.ToTable("adapter_call_audit", "spaceos_cutting");
        builder.HasKey(x => new { x.CallId, x.StartedAt });

        builder.Property(x => x.CallId)
            .HasColumnName("call_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.AdapterName)
            .HasColumnName("adapter_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.TransportName)
            .HasColumnName("transport_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.MethodName)
            .HasColumnName("method_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(200);

        builder.Property(x => x.PayloadHash)
            .HasColumnName("payload_hash")
            .HasMaxLength(128);

        builder.Property(x => x.PayloadSizeBytes)
            .HasColumnName("payload_size_bytes");

        builder.Property(x => x.StartedAt)
            .HasColumnName("started_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.DurationMs)
            .HasColumnName("duration_ms");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(8000);

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_aca_tenant_id");

        builder.HasIndex(x => new { x.TenantId, x.StartedAt })
            .HasDatabaseName("ix_aca_tenant_started_at");
    }
}
