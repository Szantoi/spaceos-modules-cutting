using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;

/// <summary>
/// Per-machine, per-hour OEE snapshot projected from execution telemetry.
/// Read-model entity — no domain events, no FSM.
/// </summary>
public sealed class MachineOEEHourly
{
    /// <summary>Surrogate primary key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Machine identifier this OEE snapshot belongs to.</summary>
    public string MachineId { get; private set; } = string.Empty;

    /// <summary>UTC timestamp truncated to the hour boundary.</summary>
    public DateTime HourSlot { get; private set; }

    /// <summary>OEE score value object (Availability × Performance × Quality).</summary>
    public OEEScore Score { get; private set; } = null!;

    /// <summary>UTC timestamp of the last projection write.</summary>
    public DateTime LastUpdatedAt { get; private set; }

    private MachineOEEHourly() { }

    /// <summary>Creates a new <see cref="MachineOEEHourly"/> with validated inputs.</summary>
    public static MachineOEEHourly Create(Guid tenantId, string machineId, DateTime hourSlot, OEEScore score)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(machineId)) throw new ArgumentException("MachineId required.", nameof(machineId));
        ArgumentNullException.ThrowIfNull(score);

        return new MachineOEEHourly
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MachineId = machineId,
            HourSlot = hourSlot,
            Score = score,
            LastUpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Replaces the OEE score when a recalculation arrives for the same hour.</summary>
    public void Update(OEEScore score)
    {
        ArgumentNullException.ThrowIfNull(score);
        Score = score;
        LastUpdatedAt = DateTime.UtcNow;
    }
}
