using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Execution.Application.Ports;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Infrastructure.StageRegistry;

/// <summary>
/// No-op stage registry. Logs milestone notifications without forwarding them.
/// Replace with a real integration when the stage registry service is available.
/// </summary>
public sealed class NullStageRegistry(ILogger<NullStageRegistry> logger) : IStageRegistry
{
    /// <inheritdoc />
    public Task NotifyMilestoneAsync(
        Guid tenantId,
        Guid executionId,
        Guid milestoneId,
        MilestoneKind kind,
        CancellationToken ct)
    {
        logger.LogWarning(
            "NullStageRegistry: milestone {Kind} ({MilestoneId}) skipped for execution {ExecutionId} tenant {TenantId}.",
            kind, milestoneId, executionId, tenantId);

        return Task.CompletedTask;
    }
}
