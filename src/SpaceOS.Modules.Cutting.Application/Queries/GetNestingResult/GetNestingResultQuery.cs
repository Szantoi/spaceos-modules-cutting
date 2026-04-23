using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Queries.GetNestingResult;

/// <summary>
/// Query to compute a nesting result for a CuttingSheet.
/// When PlanId and TenantId are provided, the result is persisted as a PlanNestingSnapshot.
/// </summary>
public sealed record GetNestingResultQuery(
    Guid SheetId,
    Guid? PlanId = null,
    Guid? TenantId = null
) : IRequest<Result<NestingResultResponse>>;
