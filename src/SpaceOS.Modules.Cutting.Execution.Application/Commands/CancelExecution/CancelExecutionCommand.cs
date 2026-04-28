using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.CancelExecution;

public sealed record CancelExecutionCommand(
    Guid ExecutionId,
    Guid TenantId,
    CancelReason Reason) : IRequest<Result>;
