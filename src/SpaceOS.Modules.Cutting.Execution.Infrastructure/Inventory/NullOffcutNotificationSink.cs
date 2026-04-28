using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Execution.Application.Ports;

namespace SpaceOS.Modules.Cutting.Execution.Infrastructure.Inventory;

/// <summary>
/// No-op offcut notification sink. Logs offcut events without forwarding them to Inventory.
/// Replace with a real HTTP adapter when the Inventory event bus is available.
/// </summary>
public sealed class NullOffcutNotificationSink(ILogger<NullOffcutNotificationSink> logger) : IOffcutNotificationSink
{
    /// <inheritdoc />
    public Task NotifyAsync(
        Guid tenantId,
        Guid executionId,
        Guid offcutId,
        decimal areaMm2,
        CancellationToken ct)
    {
        logger.LogWarning(
            "NullOffcutNotificationSink: offcut {OffcutId} ({AreaMm2} mm²) skipped for execution {ExecutionId} tenant {TenantId}.",
            offcutId, areaMm2, executionId, tenantId);

        return Task.CompletedTask;
    }
}
