namespace SpaceOS.Modules.Cutting.Application.Queries.GetCuttingPlan;

public sealed record CuttingJobResponse(
    Guid Id,
    Guid OrderId,
    string ScheduledDate,
    string Priority,
    decimal EstimatedTimeHours,
    string Status);

public sealed record DailyPlanResponse(
    Guid Id,
    string Date,
    decimal AvailableCapacity,
    decimal AllocatedCapacity,
    decimal UtilizationPercent,
    IReadOnlyList<CuttingJobResponse> Jobs);

public sealed record CuttingPlanResponse(
    Guid Id,
    string PlanDate,
    int PlanDays,
    string Status,
    string StrategyId,
    IReadOnlyList<DailyPlanResponse> DailyPlans);

public sealed record CuttingPlanSummaryResponse(
    Guid Id,
    string PlanDate,
    int PlanDays,
    string Status,
    string StrategyId);
