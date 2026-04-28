using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.EvaluateMilestones;

public sealed record EvaluateMilestonesCommand(
    Guid ExecutionId,
    Guid TenantId) : IRequest<Result>;
