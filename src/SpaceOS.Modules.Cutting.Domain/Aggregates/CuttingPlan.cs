using Ardalis.Result;
using SpaceOS.Modules.Cutting.Domain.Common;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Events;

namespace SpaceOS.Modules.Cutting.Domain.Aggregates;

/// <summary>Aggregate root for a multi-day cutting production plan.</summary>
public class CuttingPlan : AggregateRoot
{
    public Guid Id { get; private set; }
    public DateTime PlanDate { get; private set; }
    public int PlanDays { get; private set; }
    public CuttingPlanStatus Status { get; private set; } = CuttingPlanStatus.Draft;
    public string StrategyId { get; private set; } = string.Empty;
    public Guid TenantId { get; private set; }
    public Guid? ProfileSnapshotId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<DaySlot> _daySlots = new();
    public IReadOnlyList<DaySlot> DaySlots => _daySlots.AsReadOnly();

    private CuttingPlan() { }

    /// <summary>Creates a new CuttingPlan and generates <paramref name="planDays"/> child DaySlots.</summary>
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
            Status = CuttingPlanStatus.Draft,
            StrategyId = strategyId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        for (int i = 0; i < planDays; i++)
        {
            var slotDate = DateOnly.FromDateTime(plan.PlanDate.AddDays(i));
            plan._daySlots.Add(DaySlot.Create(plan.Id, slotDate));
        }

        return plan;
    }

    /// <summary>Transitions the plan to a new status.</summary>
    [Obsolete("Use Publish(), Freeze(), or Close() FSM methods instead. Will be removed in v1.4.0.")]
    public void UpdateStatus(CuttingPlanStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Draft → Published. Requires at least one DaySlot and a valid profile snapshot.</summary>
    public Result Publish(Guid profileSnapshotId)
    {
        if (Status != CuttingPlanStatus.Draft)
            return Result.Invalid(new ValidationError("Only Draft plans can be published."));
        if (!DaySlots.Any())
            return Result.Invalid(new ValidationError("Plan must have at least one DaySlot."));
        if (profileSnapshotId == Guid.Empty)
            return Result.Invalid(new ValidationError("ProfileSnapshotId is required."));

        ProfileSnapshotId = profileSnapshotId;
        Status = CuttingPlanStatus.Published;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>Published → Frozen. Requires at least one Open DaySlot.</summary>
    public Result Freeze()
    {
        if (Status != CuttingPlanStatus.Published)
            return Result.Invalid(new ValidationError("Only Published plans can be frozen."));
        if (!DaySlots.Any(s => s.Status == DaySlotStatus.Open))
            return Result.Invalid(new ValidationError("Plan must have at least one Open DaySlot."));

        Status = CuttingPlanStatus.Frozen;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new CuttingPlanFrozen(Id, TenantId, DateTimeOffset.UtcNow));
        return Result.Success();
    }

    /// <summary>Frozen → Closed. All DaySlots must be Locked or Closed.</summary>
    public Result Close()
    {
        if (Status != CuttingPlanStatus.Frozen)
            return Result.Invalid(new ValidationError("Only Frozen plans can be closed."));
        if (DaySlots.Any(s => s.Status == DaySlotStatus.Open))
            return Result.Invalid(new ValidationError("All DaySlots must be Locked or Closed before closing plan."));

        Status = CuttingPlanStatus.Closed;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
