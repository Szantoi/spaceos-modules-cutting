using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.RecordOffcut;

public sealed record RecordOffcutCommand(
    Guid ExecutionId,
    Guid TenantId,
    Guid MaterialId,
    decimal WidthMm,
    decimal HeightMm) : IRequest<Result>;
