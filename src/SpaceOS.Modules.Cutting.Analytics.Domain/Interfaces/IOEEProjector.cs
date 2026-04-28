using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;

/// <summary>Projects execution telemetry into <c>MachineOEEHourly</c> read-models.</summary>
public interface IOEEProjector
{
    /// <summary>Upserts the OEE row for the given machine/hour slot; idempotent via event dedup.</summary>
    Task ProjectAsync(
        Guid tenantId, string machineId, DateTime hourSlot,
        OEEScore score, Guid eventId, CancellationToken ct);
}
