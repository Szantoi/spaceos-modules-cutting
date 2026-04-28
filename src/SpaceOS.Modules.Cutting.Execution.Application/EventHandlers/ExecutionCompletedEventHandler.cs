using MediatR;
using SpaceOS.Modules.Cutting.Execution.Application.Ports;
using SpaceOS.Modules.Cutting.Execution.Domain.Events;

namespace SpaceOS.Modules.Cutting.Execution.Application.EventHandlers;

/// <summary>
/// Handles CuttingExecutionCompleted — the CompletionProofCommittedEventHandler appends the hash chain.
/// This handler is reserved for any additional completion side-effects (e.g. analytics).
/// </summary>
public sealed class ExecutionCompletedEventHandler : INotificationHandler<CuttingExecutionCompleted>
{
    public Task Handle(CuttingExecutionCompleted notification, CancellationToken ct)
        => Task.CompletedTask;
}
