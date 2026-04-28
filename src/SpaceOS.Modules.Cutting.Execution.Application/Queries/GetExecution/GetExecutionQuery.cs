using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Application.DTOs;

namespace SpaceOS.Modules.Cutting.Execution.Application.Queries.GetExecution;

public sealed record GetExecutionQuery(Guid ExecutionId, Guid TenantId) : IRequest<Result<ExecutionDto>>;
