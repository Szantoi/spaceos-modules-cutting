using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Queries.GetDailyCuttingPlan;

public sealed class GetDailyCuttingPlanQueryHandler : IRequestHandler<GetDailyCuttingPlanQuery, Result<DailyCuttingPlanResponse>>
{
    private readonly ICuttingRepository _repository;

    public GetDailyCuttingPlanQueryHandler(ICuttingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<DailyCuttingPlanResponse>> Handle(GetDailyCuttingPlanQuery request, CancellationToken ct)
    {
        var plan = await _repository.GetDailyCuttingPlanByDateAsync(request.PlanDate, ct).ConfigureAwait(false);
        if (plan is null)
            return Result<DailyCuttingPlanResponse>.NotFound($"No cutting plan found for {request.PlanDate:yyyy-MM-dd}.");

        var batches = plan.Batches
            .Select(b => new CuttingBatchResponse(b.MaterialType, b.ThicknessMm, b.SheetIds.ToList()))
            .ToList();

        return Result<DailyCuttingPlanResponse>.Success(new DailyCuttingPlanResponse(
            plan.Id, plan.Name, plan.PlanDate, plan.Status.ToString(), batches));
    }
}
