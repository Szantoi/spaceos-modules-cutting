using SpaceOS.Modules.Cutting.Application.Queries.GetCuttingPlan;

namespace SpaceOS.Modules.Cutting.Application.Commands.CreateCuttingPlan;

/// <summary>Response returned after a cutting plan is successfully created.</summary>
public sealed record CreateCuttingPlanResponse(
    Guid PlanId,
    IReadOnlyList<DailyPlanResponse> DailyPlans,
    IReadOnlyList<CuttingJobResponse> ScheduledJobs,
    decimal TotalYieldPercent);
