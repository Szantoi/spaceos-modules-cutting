using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Domain.Entities;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations;

public class BatchAssignmentConfiguration : IEntityTypeConfiguration<BatchAssignment>
{
    public void Configure(EntityTypeBuilder<BatchAssignment> builder)
    {
        builder.ToTable("BatchAssignments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.BatchId).IsRequired();
        builder.Property(x => x.PlanDate).IsRequired();
        builder.Property(x => x.MachineId).IsRequired();
        builder.Property(x => x.OperatorId).IsRequired();
        builder.Property(x => x.ExecutionId).IsRequired();
        builder.Property(x => x.Priority).IsRequired();
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        // Unique constraint for idempotency: (BatchId, PlanDate)
        builder.HasIndex(x => new { x.BatchId, x.PlanDate })
            .IsUnique()
            .HasDatabaseName("IX_BatchAssignments_BatchId_PlanDate");

        // Index for tenant-based queries
        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("IX_BatchAssignments_TenantId");
    }
}
