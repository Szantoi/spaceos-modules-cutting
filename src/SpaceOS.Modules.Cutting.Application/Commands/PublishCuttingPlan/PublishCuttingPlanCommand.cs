using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Commands.PublishCuttingPlan;

public sealed record PublishCuttingPlanCommand(Guid PlanId, Guid ProfileSnapshotId) : IRequest<Result<Unit>>;
