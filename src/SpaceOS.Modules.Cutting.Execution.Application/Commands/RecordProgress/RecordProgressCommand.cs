using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.RecordProgress;

public sealed record RecordProgressCommand(
    Guid ExecutionId,
    Guid TenantId,
    Guid EventId,
    ProgressEventKind Kind,
    int? Panel,
    DateTime OccurredAt,
    string EventHmacBase64,
    string HmacKeyVersion) : IRequest<Result>;
