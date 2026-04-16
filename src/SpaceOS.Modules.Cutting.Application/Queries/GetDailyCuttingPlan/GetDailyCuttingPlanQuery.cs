using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Queries.GetDailyCuttingPlan;

public sealed record GetDailyCuttingPlanQuery(DateTime PlanDate) : IRequest<Result<DailyCuttingPlanResponse>>;
