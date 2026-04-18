using SpaceOS.Modules.Cutting.Domain.Common;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Domain.Aggregates;

public class DailyCuttingPlan : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime PlanDate { get; private set; }
    public DailyPlanStatus Status { get; private set; }
    private readonly List<CuttingBatch> _batches = new();
    public IReadOnlyList<CuttingBatch> Batches => _batches.AsReadOnly();

    private DailyCuttingPlan() { }

    public static DailyCuttingPlan Create(Guid tenantId, string name, DateTime planDate, IEnumerable<CuttingBatch> batches)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));

        var plan = new DailyCuttingPlan
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            PlanDate = planDate.Date,
            Status = DailyPlanStatus.Draft
        };
        plan._batches.AddRange(batches);
        return plan;
    }

    public void FinalizePlan()
    {
        if (Status == DailyPlanStatus.Finalized)
            throw new InvalidOperationException("Plan is already finalized.");
        Status = DailyPlanStatus.Finalized;
    }
}
