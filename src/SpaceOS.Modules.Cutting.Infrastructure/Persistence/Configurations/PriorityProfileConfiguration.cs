using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations;

public class PriorityProfileConfiguration : IEntityTypeConfiguration<PriorityProfile>
{
    public void Configure(EntityTypeBuilder<PriorityProfile> builder)
    {
        builder.ToTable("PriorityProfiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.IsDefault).IsRequired();
        builder.Property(x => x.CapacityModelId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ReworkPolicyId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.PlanningStrategyId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.OwnsMany(x => x.Rules, rules =>
        {
            rules.ToJson();
            rules.Property(r => r.Order).IsRequired();
            rules.Property(r => r.RuleName).IsRequired().HasMaxLength(100);
            rules.Property(r => r.Parameter).HasMaxLength(200);
        });

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.IsDefault });
    }
}
