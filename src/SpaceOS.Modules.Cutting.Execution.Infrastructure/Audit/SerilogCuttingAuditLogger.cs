using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Execution.Application.Ports;

namespace SpaceOS.Modules.Cutting.Execution.Infrastructure.Audit;

/// <summary>
/// Serilog-backed audit logger for security-sensitive cutting execution events.
/// All security events are emitted at Warning level to ensure they are captured
/// by any log-level configuration.
/// </summary>
public sealed class SerilogCuttingAuditLogger(ILogger<SerilogCuttingAuditLogger> logger) : ICuttingAuditLogger
{
    /// <inheritdoc />
    public Task LogSecurityEventAsync(
        string eventType,
        Guid tenantId,
        Guid executionId,
        Guid eventId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogWarning(
            "SECURITY EVENT: {EventType} tenant={TenantId} execution={ExecutionId} eventId={EventId}",
            eventType, tenantId, executionId, eventId);

        return Task.CompletedTask;
    }
}
