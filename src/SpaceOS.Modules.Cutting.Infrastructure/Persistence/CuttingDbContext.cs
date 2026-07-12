using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Domain.Adapters;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters;
using SpaceOS.Modules.Cutting.Infrastructure.Outbox;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations.Adapters;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations.Execution;
using ExecutionAggregate = SpaceOS.Modules.Cutting.Execution.Domain.Aggregates.CuttingExecution;
using ExecutionEntity_ProgressEvent = SpaceOS.Modules.Cutting.Execution.Domain.Entities.ProgressEvent;
using ExecutionEntity_OffcutReport = SpaceOS.Modules.Cutting.Execution.Domain.Entities.OffcutReport;
using ExecutionEntity_MilestoneSubscription = SpaceOS.Modules.Cutting.Execution.Domain.Entities.MilestoneSubscription;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence;

public class CuttingDbContext : DbContext
{
    public CuttingDbContext(DbContextOptions<CuttingDbContext> options) : base(options) { }

    // Phase 3 — planning & sheet domain
    public DbSet<CuttingSheet> CuttingSheets => Set<CuttingSheet>();
    public DbSet<CuttingLine> CuttingLines => Set<CuttingLine>();
    public DbSet<DailyCuttingPlan> DailyCuttingPlans => Set<DailyCuttingPlan>();
    public DbSet<CuttingBatch> CuttingBatches => Set<CuttingBatch>();
    public DbSet<BatchAssignment> BatchAssignments => Set<BatchAssignment>();

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

    // Q3 — Quote Request (public customer self-service)
    public DbSet<CuttingQuoteRequest> QuoteRequests => Set<CuttingQuoteRequest>();

    // Q3 Track A — Public Quote Request (B2C, MSG-BACKEND-030)
    public DbSet<PublicQuoteRequest> PublicQuoteRequests => Set<PublicQuoteRequest>();

    // Q3 Track B — Pricing
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<MaterialPricing> MaterialPricings => Set<MaterialPricing>();
    public DbSet<ComplexityModifier> ComplexityModifiers => Set<ComplexityModifier>();

    // Q3 Track B Phase 1 — Pricing Rule Engine (MSG-BACKEND-031)
    public DbSet<PricingRule> PricingRules => Set<PricingRule>();
    public DbSet<QuantityBreakpoint> QuantityBreakpoints => Set<QuantityBreakpoint>();
    public DbSet<LeadTimeAdjustment> LeadTimeAdjustments => Set<LeadTimeAdjustment>();
    public DbSet<MaterialSurcharge> MaterialSurcharges => Set<MaterialSurcharge>();

    // Phase 4 — Execution aggregate
    public DbSet<ExecutionAggregate> CuttingExecutions => Set<ExecutionAggregate>();
    public DbSet<ExecutionEntity_ProgressEvent> ProgressEvents => Set<ExecutionEntity_ProgressEvent>();
    public DbSet<ExecutionEntity_OffcutReport> OffcutReports => Set<ExecutionEntity_OffcutReport>();
    public DbSet<ExecutionEntity_MilestoneSubscription> MilestoneSubscriptions => Set<ExecutionEntity_MilestoneSubscription>();

    // Phase 4 — Local outbox
    public DbSet<LocalOutboxMessage> LocalOutboxMessages => Set<LocalOutboxMessage>();

    // Phase 6 — Adapter domain
    public DbSet<TenantCuttingProviderConfig> TenantCuttingProviderConfigs => Set<TenantCuttingProviderConfig>();
    public DbSet<AdapterHealthRecord> AdapterHealthRecords => Set<AdapterHealthRecord>();
    public DbSet<AdapterCallAuditEntity> AdapterCallAudits => Set<AdapterCallAuditEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("spaceos_cutting");

        // Phase 3 configurations
        modelBuilder.ApplyConfiguration(new CuttingSheetConfiguration());
        modelBuilder.ApplyConfiguration(new CuttingLineConfiguration());
        modelBuilder.ApplyConfiguration(new DailyCuttingPlanConfiguration());
        modelBuilder.ApplyConfiguration(new CuttingBatchConfiguration());
        modelBuilder.ApplyConfiguration(new BatchAssignmentConfiguration());
        modelBuilder.ApplyConfiguration(new CuttingPlanConfiguration());
        modelBuilder.ApplyConfiguration(new DaySlotConfiguration());
        modelBuilder.ApplyConfiguration(new CuttingJobConfiguration());
        modelBuilder.ApplyConfiguration(new PriorityProfileConfiguration());
        modelBuilder.ApplyConfiguration(new PanelReservationConfiguration());
        modelBuilder.ApplyConfiguration(new PlanNestingSnapshotConfiguration());
        modelBuilder.ApplyConfiguration(new CuttingQuoteRequestConfiguration());
        modelBuilder.ApplyConfiguration(new PublicQuoteRequestConfiguration());

        // Phase 4 configurations
        modelBuilder.ApplyConfiguration(new ExecutionCuttingExecutionConfiguration());
        modelBuilder.ApplyConfiguration(new LocalOutboxMessageConfiguration());

        // Phase 6 configurations
        modelBuilder.ApplyConfiguration(new TenantCuttingProviderConfigConfiguration());
        modelBuilder.ApplyConfiguration(new AdapterHealthRecordConfiguration());
        modelBuilder.ApplyConfiguration(new AdapterCallAuditConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
