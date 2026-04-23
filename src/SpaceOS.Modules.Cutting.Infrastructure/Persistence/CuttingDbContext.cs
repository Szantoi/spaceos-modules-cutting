using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence;

public class CuttingDbContext : DbContext
{
    public CuttingDbContext(DbContextOptions<CuttingDbContext> options) : base(options) { }

    public DbSet<CuttingSheet> CuttingSheets => Set<CuttingSheet>();
    public DbSet<CuttingLine> CuttingLines => Set<CuttingLine>();
    public DbSet<DailyCuttingPlan> DailyCuttingPlans => Set<DailyCuttingPlan>();
    public DbSet<CuttingBatch> CuttingBatches => Set<CuttingBatch>();
    public DbSet<CuttingExecution> CuttingExecutions => Set<CuttingExecution>();

    // CuttingPlan aggregate (planning)
    public DbSet<CuttingPlan> CuttingPlans => Set<CuttingPlan>();
    public DbSet<DaySlot> DaySlots => Set<DaySlot>();
    public DbSet<CuttingJob> CuttingJobs => Set<CuttingJob>();

    // PriorityProfile
    public DbSet<PriorityProfile> PriorityProfiles => Set<PriorityProfile>();

    // PanelReservation
    public DbSet<PanelReservation> PanelReservations => Set<PanelReservation>();

    // PlanNestingSnapshot
    public DbSet<PlanNestingSnapshot> PlanNestingSnapshots => Set<PlanNestingSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("spaceos_cutting");
        modelBuilder.ApplyConfiguration(new CuttingSheetConfiguration());
        modelBuilder.ApplyConfiguration(new CuttingLineConfiguration());
        modelBuilder.ApplyConfiguration(new DailyCuttingPlanConfiguration());
        modelBuilder.ApplyConfiguration(new CuttingBatchConfiguration());
        modelBuilder.ApplyConfiguration(new CuttingExecutionConfiguration());
        modelBuilder.ApplyConfiguration(new CuttingPlanConfiguration());
        modelBuilder.ApplyConfiguration(new DaySlotConfiguration());
        modelBuilder.ApplyConfiguration(new CuttingJobConfiguration());
        modelBuilder.ApplyConfiguration(new PriorityProfileConfiguration());
        modelBuilder.ApplyConfiguration(new PanelReservationConfiguration());
        modelBuilder.ApplyConfiguration(new PlanNestingSnapshotConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
