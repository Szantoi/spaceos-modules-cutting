using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Application.DTOs;

namespace SpaceOS.Modules.Cutting.Execution.Application.Queries.GetMilestones;

public sealed record GetMilestonesQuery(Guid ExecutionId, Guid TenantId) : IRequest<Result<IReadOnlyList<MilestoneDto>>>;
