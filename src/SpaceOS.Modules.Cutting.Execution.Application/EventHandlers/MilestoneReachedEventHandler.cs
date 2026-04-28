using MediatR;
using SpaceOS.Modules.Cutting.Execution.Application.Ports;
using SpaceOS.Modules.Cutting.Execution.Domain.Events;

namespace SpaceOS.Modules.Cutting.Execution.Application.EventHandlers;

public sealed class MilestoneReachedEventHandler(IStageRegistry stageRegistry)
    : INotificationHandler<MilestoneReached>
{
    public async Task Handle(MilestoneReached notification, CancellationToken ct)
    {
        await stageRegistry.NotifyMilestoneAsync(
            notification.TenantId,
            notification.ExecutionId,
            notification.MilestoneId,
            notification.Kind,
            ct).ConfigureAwait(false);
    }
}
