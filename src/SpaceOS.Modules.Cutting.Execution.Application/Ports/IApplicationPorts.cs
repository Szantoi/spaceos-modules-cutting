using SpaceOS.Modules.Cutting.Execution.Application.DTOs;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Application.Ports;

/// <summary>Pushes real-time progress updates to connected clients.</summary>
public interface ICuttingExecutionRealtimePublisher
{
    Task PublishProgressAsync(Guid tenantId, Guid executionId, ProgressEventDto evt, CancellationToken ct);
    Task PublishPanelCompletedAsync(Guid tenantId, Guid executionId, int panelNumber, int total, CancellationToken ct);
}

/// <summary>Appends a hash to the immutable hash chain for an execution.</summary>
public interface ICuttingHashChainSink
{
    Task AppendAsync(Guid tenantId, Guid executionId, string proofHash, CancellationToken ct);
}

/// <summary>Notifies the stage registry when a milestone is reached.</summary>
public interface IStageRegistry
{
    Task NotifyMilestoneAsync(Guid tenantId, Guid executionId, Guid milestoneId, MilestoneKind kind, CancellationToken ct);
}

/// <summary>Sends an offcut notification to the Inventory module.</summary>
public interface IOffcutNotificationSink
{
    Task NotifyAsync(Guid tenantId, Guid executionId, Guid offcutId, decimal areaMm2, CancellationToken ct);
}

/// <summary>Logs security-sensitive events for audit trail.</summary>
public interface ICuttingAuditLogger
{
    Task LogSecurityEventAsync(string eventType, Guid tenantId, Guid executionId, Guid eventId, CancellationToken ct);
}
