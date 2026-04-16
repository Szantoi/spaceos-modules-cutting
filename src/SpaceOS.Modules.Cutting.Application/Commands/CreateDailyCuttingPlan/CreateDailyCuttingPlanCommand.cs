using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Commands.CreateDailyCuttingPlan;

public sealed record CuttingBatchInput(string MaterialType, decimal ThicknessMm, IReadOnlyList<Guid> SheetIds);

public sealed record CreateDailyCuttingPlanCommand(
    Guid TenantId,
    DateTime PlanDate,
    IReadOnlyList<CuttingBatchInput> Batches) : IRequest<Result<Guid>>;
