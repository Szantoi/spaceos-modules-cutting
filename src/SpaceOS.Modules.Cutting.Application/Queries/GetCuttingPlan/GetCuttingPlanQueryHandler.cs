using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Queries.GetCuttingPlan;

public sealed class GetCuttingPlanQueryHandler : IRequestHandler<GetCuttingPlanQuery, Result<CuttingPlanResponse>>
{
    private readonly ICuttingRepository _repository;

    public GetCuttingPlanQueryHandler(ICuttingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CuttingPlanResponse>> Handle(GetCuttingPlanQuery request, CancellationToken ct)
    {
        var plan = await _repository.GetCuttingPlanByIdAsync(request.PlanId, ct).ConfigureAwait(false);
        if (plan is null)
            return Result<CuttingPlanResponse>.NotFound($"CuttingPlan {request.PlanId} not found.");

        var dailyPlans = plan.DaySlots.Select(d => new DailyPlanResponse(
            d.Id,
            d.SlotDate.ToString("yyyy-MM-dd"),
            d.CapacityHours,
            d.UsedCapacityHours,
            d.UtilizationPercent,
            d.Jobs.Select(j => new CuttingJobResponse(
                j.Id,
                j.OrderId,
                j.ScheduledDate.ToString("yyyy-MM-dd"),
                j.Priority,
                j.EstimatedTimeHours,
                j.Status)).ToList()
        )).ToList();

        var response = new CuttingPlanResponse(
            plan.Id,
            plan.PlanDate.ToString("yyyy-MM-dd"),
            plan.PlanDays,
            plan.Status.ToString(),
            plan.StrategyId,
            dailyPlans);

        return Result<CuttingPlanResponse>.Success(response);
    }
}
