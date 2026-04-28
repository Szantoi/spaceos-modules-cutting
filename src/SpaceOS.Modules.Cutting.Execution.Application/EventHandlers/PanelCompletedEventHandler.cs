using MediatR;
using SpaceOS.Modules.Cutting.Execution.Application.Ports;
using SpaceOS.Modules.Cutting.Execution.Domain.Events;

namespace SpaceOS.Modules.Cutting.Execution.Application.EventHandlers;

public sealed class PanelCompletedEventHandler(ICuttingExecutionRealtimePublisher publisher)
    : INotificationHandler<PanelCompleted>
{
    public async Task Handle(PanelCompleted notification, CancellationToken ct)
    {
        await publisher.PublishPanelCompletedAsync(
            notification.TenantId,
            notification.ExecutionId,
            notification.PanelNumber,
            notification.TotalPanels,
            ct).ConfigureAwait(false);
    }
}
