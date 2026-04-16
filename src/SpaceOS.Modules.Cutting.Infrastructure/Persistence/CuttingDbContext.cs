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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("spaceos_cutting");
        modelBuilder.ApplyConfiguration(new CuttingSheetConfiguration());
        modelBuilder.ApplyConfiguration(new CuttingLineConfiguration());
        modelBuilder.ApplyConfiguration(new DailyCuttingPlanConfiguration());
        modelBuilder.ApplyConfiguration(new CuttingBatchConfiguration());
        modelBuilder.ApplyConfiguration(new CuttingExecutionConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
