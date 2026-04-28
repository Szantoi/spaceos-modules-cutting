using Microsoft.AspNetCore.SignalR;
using SpaceOS.Modules.Cutting.Execution.Application.DTOs;
using SpaceOS.Modules.Cutting.Execution.Application.Ports;

namespace SpaceOS.Modules.Cutting.Execution.Infrastructure.Realtime;

/// <summary>
/// Publishes real-time execution progress updates to clients via SignalR.
/// Tenant isolation is enforced by scoping group names to tenantId:executionId.
/// </summary>
public sealed class SignalRCuttingExecutionRealtimePublisher(
    IHubContext<ExecutionHub> hubContext) : ICuttingExecutionRealtimePublisher
{
    /// <inheritdoc />
    public async Task PublishProgressAsync(
        Guid tenantId,
        Guid executionId,
        ProgressEventDto evt,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(evt);

        var group = BuildGroupName(tenantId, executionId);
        await hubContext.Clients.Group(group)
            .SendAsync("progress", evt, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task PublishPanelCompletedAsync(
        Guid tenantId,
        Guid executionId,
        int panelNumber,
        int total,
        CancellationToken ct)
    {
        var group = BuildGroupName(tenantId, executionId);
        await hubContext.Clients.Group(group)
            .SendAsync("panelCompleted", new { panelNumber, total }, ct)
            .ConfigureAwait(false);
    }

    private static string BuildGroupName(Guid tenantId, Guid executionId)
        => $"{tenantId}:{executionId}";
}
