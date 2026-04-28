using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.StartExecution;

public sealed record StartExecutionCommand(
    Guid ExecutionId,
    Guid TenantId,
    Guid WorkerId,
    string BadgeHmacBase64,
    string HmacKeyVersion) : IRequest<Result>;
