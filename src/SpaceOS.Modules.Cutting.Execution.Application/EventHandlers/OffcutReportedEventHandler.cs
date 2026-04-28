using MediatR;
using SpaceOS.Modules.Cutting.Execution.Application.Ports;
using SpaceOS.Modules.Cutting.Execution.Domain.Events;

namespace SpaceOS.Modules.Cutting.Execution.Application.EventHandlers;

public sealed class OffcutReportedEventHandler(IOffcutNotificationSink offcutSink)
    : INotificationHandler<OffcutReported>
{
    public async Task Handle(OffcutReported notification, CancellationToken ct)
    {
        await offcutSink.NotifyAsync(
            notification.TenantId,
            notification.ExecutionId,
            notification.OffcutId,
            notification.AreaMm2,
            ct).ConfigureAwait(false);
    }
}
