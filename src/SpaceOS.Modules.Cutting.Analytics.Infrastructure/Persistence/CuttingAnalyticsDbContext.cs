using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Analytics.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Analytics.Domain.Common;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;

namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the <c>cutting_analytics</c> schema.
/// Hosts all analytics read-models and the rebuild job aggregate.
/// </summary>
public sealed class CuttingAnalyticsDbContext(DbContextOptions<CuttingAnalyticsDbContext> options)
    : DbContext(options)
{
    /// <summary>Per-machine, per-day execution summaries.</summary>
    public DbSet<DailyExecutionMetric> DailyExecutionMetrics => Set<DailyExecutionMetric>();

    /// <summary>Per-material, per-day consumption summaries.</summary>
    public DbSet<DailyMaterialUsage> DailyMaterialUsages => Set<DailyMaterialUsage>();

    /// <summary>Per-machine, per-hour OEE snapshots.</summary>
    public DbSet<MachineOEEHourly> MachineOEEHourlies => Set<MachineOEEHourly>();

    /// <summary>Per-worker, per-day operator metrics (with k-anonymity suppression).</summary>
    public DbSet<DailyOperatorMetric> DailyOperatorMetrics => Set<DailyOperatorMetric>();

    /// <summary>Analytics rebuild jobs (FSM aggregate).</summary>
    public DbSet<AnalyticsRebuildJob> AnalyticsRebuildJobs => Set<AnalyticsRebuildJob>();

    /// <summary>Idempotency dedup ledger for outbox event projections.</summary>
    public DbSet<ProcessedOutboxEvent> ProcessedOutboxEvents => Set<ProcessedOutboxEvent>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("cutting_analytics");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CuttingAnalyticsDbContext).Assembly);
    }
}
