using MediatR;
using SpaceOS.Modules.Cutting.Execution.Application.DTOs;
using SpaceOS.Modules.Cutting.Execution.Application.Ports;
using SpaceOS.Modules.Cutting.Execution.Domain.Events;

namespace SpaceOS.Modules.Cutting.Execution.Application.EventHandlers;

public sealed class ProgressRecordedEventHandler(ICuttingExecutionRealtimePublisher publisher)
    : INotificationHandler<ProgressRecorded>
{
    public async Task Handle(ProgressRecorded notification, CancellationToken ct)
    {
        var dto = new ProgressEventDto(
            notification.EventId,
            notification.Kind.ToString(),
            null,
            notification.OccurredAt);

        await publisher.PublishProgressAsync(
            notification.TenantId,
            notification.ExecutionId,
            dto,
            ct).ConfigureAwait(false);
    }
}
