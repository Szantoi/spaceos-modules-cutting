using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Cutting.Application.Commands.AssignBatch;

/// <summary>
/// Assigns a batch to a machine and operator for a specific date.
/// Creates a <see cref="BatchAssignment"/> record and a <see cref="CuttingExecution"/> for the batch.
/// </summary>
public sealed record AssignBatchCommand(
    Guid TenantId,
    DateOnly PlanDate,
    Guid BatchId,
    Guid MachineId,
    Guid OperatorId,
    int Priority,
    DateTime StartTime) : IRequest<Result<AssignBatchResponse>>;

/// <summary>Response containing the created execution ID and status.</summary>
public sealed record AssignBatchResponse(Guid ExecutionId, string Status);
