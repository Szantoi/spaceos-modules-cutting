using MediatR;
using SpaceOS.Modules.Cutting.Execution.Application.Ports;
using SpaceOS.Modules.Cutting.Execution.Domain.Events;

namespace SpaceOS.Modules.Cutting.Execution.Application.EventHandlers;

public sealed class CompletionProofCommittedEventHandler(ICuttingHashChainSink hashChainSink)
    : INotificationHandler<CompletionProofCommitted>
{
    public async Task Handle(CompletionProofCommitted notification, CancellationToken ct)
    {
        await hashChainSink.AppendAsync(
            notification.TenantId,
            notification.ExecutionId,
            notification.ProofHash,
            ct).ConfigureAwait(false);
    }
}
