using SpaceOS.Modules.Cutting.Domain.Common;

namespace SpaceOS.Modules.Cutting.Domain.Aggregates;

/// <summary>Aggregate root for a multi-day cutting production plan.</summary>
public class CuttingPlan : AggregateRoot
{
    public Guid Id { get; private set; }
    public DateTime PlanDate { get; private set; }
    public int PlanDays { get; private set; }
    public string Status { get; private set; } = "Draft";
    public string StrategyId { get; private set; } = string.Empty;
    public Guid TenantId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<DailyPlan> _dailyPlans = new();
    public IReadOnlyList<DailyPlan> DailyPlans => _dailyPlans.AsReadOnly();

    private CuttingPlan() { }

    /// <summary>Creates a new CuttingPlan and generates <paramref name="planDays"/> child DailyPlans.</summary>
    public static CuttingPlan Create(Guid tenantId, DateTime planDate, int planDays, string strategyId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));
        if (planDays < 7 || planDays > 90) throw new ArgumentException("PlanDays must be 7-90.", nameof(planDays));
        if (string.IsNullOrWhiteSpace(strategyId)) throw new ArgumentException("StrategyId required.", nameof(strategyId));
        if (planDate.Date < DateTime.UtcNow.Date) throw new ArgumentException("PlanDate must be >= today.", nameof(planDate));

        var plan = new CuttingPlan
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlanDate = DateTime.SpecifyKind(planDate.Date, DateTimeKind.Utc),
            PlanDays = planDays,
            Status = "Draft",
            StrategyId = strategyId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        for (int i = 0; i < planDays; i++)
        {
            var dailyDate = plan.PlanDate.AddDays(i);
            plan._dailyPlans.Add(DailyPlan.Create(plan.Id, dailyDate));
        }

        return plan;
    }

    /// <summary>Transitions the plan to a new status. Valid values: Draft, Approved, InProgress, Closed.</summary>
    public void UpdateStatus(string newStatus)
    {
        if (newStatus != "Draft" && newStatus != "Approved" && newStatus != "InProgress" && newStatus != "Closed")
            throw new ArgumentException("Invalid status.", nameof(newStatus));
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}
