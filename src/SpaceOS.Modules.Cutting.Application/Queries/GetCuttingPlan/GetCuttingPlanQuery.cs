using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Queries.GetCuttingPlan;

public sealed record GetCuttingPlanQuery(Guid PlanId) : IRequest<Result<CuttingPlanResponse>>;
