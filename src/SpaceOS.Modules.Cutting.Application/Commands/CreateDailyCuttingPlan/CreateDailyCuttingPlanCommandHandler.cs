using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Commands.CreateDailyCuttingPlan;

public sealed class CreateDailyCuttingPlanCommandHandler : IRequestHandler<CreateDailyCuttingPlanCommand, Result<Guid>>
{
    private readonly ICuttingRepository _repository;

    public CreateDailyCuttingPlanCommandHandler(ICuttingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateDailyCuttingPlanCommand request, CancellationToken ct)
    {
        var tempPlanId = Guid.NewGuid();
        var batches = (request.Batches ?? []).Select(b => CuttingBatch.Create(tempPlanId, b.MaterialType, b.ThicknessMm, b.SheetIds));
        var plan = DailyCuttingPlan.Create(request.TenantId, request.Name, request.PlanDate, batches);
        plan.PopDomainEvents();

        await _repository.AddDailyCuttingPlanAsync(plan, ct).ConfigureAwait(false);
        await _repository.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result<Guid>.Success(plan.Id);
    }
}
