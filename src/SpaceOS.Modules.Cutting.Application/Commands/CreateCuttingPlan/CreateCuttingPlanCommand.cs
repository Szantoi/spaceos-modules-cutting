using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Commands.CreateCuttingPlan;

public sealed record CreateCuttingPlanCommand(
    Guid TenantId,
    DateTime PlanDate,
    int PlanDays,
    string StrategyId) : IRequest<Result<CreateCuttingPlanResponse>>;
