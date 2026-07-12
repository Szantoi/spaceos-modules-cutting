using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations.Execution;

/// <summary>
/// EF Core configuration for the <see cref="CuttingExecution"/> aggregate and its owned collections.
/// Replaces the Phase 3 stub configuration.
/// </summary>
public sealed class ExecutionCuttingExecutionConfiguration
    : IEntityTypeConfiguration<CuttingExecution>
{
    public void Configure(EntityTypeBuilder<CuttingExecution> builder)
    {
        builder.ToTable("CuttingExecutions", "spaceos_cutting");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.BatchId); // Nullable for backward compatibility
        builder.Property(x => x.SheetId).IsRequired();
        builder.Property(x => x.MachineId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Priority); // Nullable for backward compatibility

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.TotalPanels).IsRequired();
        builder.Property(x => x.PanelsCompleted).IsRequired();
        builder.Property(x => x.ScheduledAt).IsRequired();
        builder.Property(x => x.StartedAt);
        builder.Property(x => x.CompletedAt);
        builder.Property(x => x.CancelledAt);

        builder.Property(x => x.CancelReason)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.OffcutAreaMm2).HasPrecision(14, 4);
        builder.Property(x => x.TotalAreaMm2).HasPrecision(14, 4);
        builder.Property(x => x.WorkerConsentActive).IsRequired();

        // WorkerAssignment owned value object
        builder.OwnsOne(x => x.WorkerAssignment, wa =>
        {
            wa.Property(w => w.WorkerId).HasColumnName("WorkerId");
            wa.Property(w => w.EnrollmentId).HasColumnName("EnrollmentId");
        });

        // ScheduleWindow owned value object
        builder.OwnsOne(x => x.ScheduleWindow, sw =>
        {
            sw.Property(w => w.Start).HasColumnName("WindowStart");
            sw.Property(w => w.End).HasColumnName("WindowEnd");
        });

        // CompletionProof optional owned value object
        builder.OwnsOne(x => x.CompletionProof, cp =>
        {
            cp.Property(p => p.Level)
                .HasColumnName("ProofLevel")
                .HasConversion<int>();
            cp.Property(p => p.ProofHash)
                .HasColumnName("ProofHash")
                .HasMaxLength(128);
            cp.Property(p => p.Signature)
                .HasColumnName("ProofSignature")
                .HasMaxLength(512);
            cp.Property(p => p.BlobRef)
                .HasColumnName("ProofBlobRef")
                .HasMaxLength(512);
            cp.Property(p => p.EncryptedWith)
                .HasColumnName("ProofEncryptedWith")
                .HasMaxLength(128);
        });

        // ProgressEvents — owned collection with separator table
        builder.OwnsMany(x => x.ProgressEvents, pe =>
        {
            pe.ToTable("ProgressEvents", "spaceos_cutting");
            pe.WithOwner().HasForeignKey("ExecutionId");
            pe.HasKey("ExecutionId", "EventId");
            pe.Property(p => p.EventId).IsRequired().ValueGeneratedNever();
            pe.Property(p => p.Kind)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
            pe.Property(p => p.Panel);
            pe.Property(p => p.OccurredAt).IsRequired();
            pe.Property(p => p.EventHmac).HasMaxLength(256).IsRequired();
            pe.HasIndex("ExecutionId");
            pe.HasIndex(p => p.EventId).IsUnique();
        });

        // OffcutReports — owned collection
        builder.OwnsMany(x => x.OffcutReports, or =>
        {
            or.ToTable("OffcutReports", "spaceos_cutting");
            or.WithOwner().HasForeignKey("ExecutionId");
            or.HasKey("ExecutionId", "OffcutId");
            or.Property(p => p.OffcutId).IsRequired().ValueGeneratedNever();
            or.Property(p => p.MaterialId).IsRequired();
            or.Property(p => p.WidthMm).HasPrecision(14, 4).IsRequired();
            or.Property(p => p.HeightMm).HasPrecision(14, 4).IsRequired();
            or.Property(p => p.AreaMm2).HasPrecision(14, 4).IsRequired();
            or.Property(p => p.OccurredAt).IsRequired();
            or.HasIndex("ExecutionId");
        });

        // MilestoneSubscriptions — owned collection
        builder.OwnsMany(x => x.Milestones, ms =>
        {
            ms.ToTable("MilestoneSubscriptions", "spaceos_cutting");
            ms.WithOwner().HasForeignKey("ExecutionId");
            ms.HasKey("ExecutionId", "MilestoneId");
            ms.Property(p => p.MilestoneId).IsRequired().ValueGeneratedNever();
            ms.Property(p => p.Kind)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            ms.Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            ms.Property(p => p.ConfigJson).IsRequired();
            ms.Property(p => p.ConfigVersion).IsRequired();
            ms.Property(p => p.ReachedAt);
            ms.HasIndex("ExecutionId");
        });

        // Indexes on the root table
        builder.HasIndex(x => x.TenantId).HasDatabaseName("IX_CuttingExecutions_TenantId");
        builder.HasIndex(x => x.SheetId).HasDatabaseName("IX_CuttingExecutions_SheetId");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_CuttingExecutions_Status");
    }
}
