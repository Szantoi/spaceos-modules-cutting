using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Domain.Adapters;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations.Adapters;

internal sealed class TenantCuttingProviderConfigConfiguration
    : IEntityTypeConfiguration<TenantCuttingProviderConfig>
{
    public void Configure(EntityTypeBuilder<TenantCuttingProviderConfig> builder)
    {
        builder.ToTable("tenant_cutting_provider_config", "spaceos_cutting");
        builder.HasKey(x => x.TenantId);

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

        builder.Property(x => x.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired();

        builder.Property(x => x.ConfigJson)
            .HasColumnName("config_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.ConfigSchemaVersion)
            .HasColumnName("config_schema_version")
            .IsRequired();

        builder.Property(x => x.Version)
            .HasColumnName("version")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasIndex(x => x.AdapterName)
            .HasDatabaseName("ix_tcpc_adapter_name");

        builder.HasIndex(x => x.IsEnabled)
            .HasDatabaseName("ix_tcpc_is_enabled");
    }
}
