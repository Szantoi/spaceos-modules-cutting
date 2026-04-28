using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Application.DTOs;

namespace SpaceOS.Modules.Cutting.Execution.Application.Queries.GetProgress;

public sealed record GetProgressQuery(Guid ExecutionId, Guid TenantId) : IRequest<Result<IReadOnlyList<ProgressEventDto>>>;
