using SpaceOS.Modules.Cutting.Domain.Common;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Events;

namespace SpaceOS.Modules.Cutting.Domain.Aggregates;

[Obsolete("Phase 3 execution stub. Use SpaceOS.Modules.Cutting.Execution.Domain.Aggregates.CuttingExecution instead (Phase 4).")]
public class CuttingExecution : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CuttingSheetId { get; private set; }
    public string AssignedTo { get; private set; } = string.Empty;
    public ExecutionStatus Status { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public decimal WasteAreaCm2 { get; private set; }

    private CuttingExecution() { }

    public static CuttingExecution Plan(Guid tenantId, Guid cuttingSheetId, string assignedTo)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));
        if (cuttingSheetId == Guid.Empty) throw new ArgumentException("CuttingSheetId required.", nameof(cuttingSheetId));
        ArgumentException.ThrowIfNullOrWhiteSpace(assignedTo);

        return new CuttingExecution
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CuttingSheetId = cuttingSheetId,
            AssignedTo = assignedTo,
            Status = ExecutionStatus.Planned
        };
    }

    public void Start()
    {
        if (Status != ExecutionStatus.Planned)
            throw new InvalidOperationException($"Cannot start execution in status {Status}.");
        Status = ExecutionStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete(decimal wasteAreaCm2)
    {
        if (Status != ExecutionStatus.InProgress)
            throw new InvalidOperationException($"Cannot complete execution in status {Status}.");
        if (wasteAreaCm2 < 0) throw new ArgumentException("Waste area cannot be negative.", nameof(wasteAreaCm2));
        Status = ExecutionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        WasteAreaCm2 = wasteAreaCm2;
        RaiseDomainEvent(new CuttingExecutionCompletedEvent(Id, TenantId, CuttingSheetId, wasteAreaCm2));
        if (wasteAreaCm2 > 0)
            RaiseDomainEvent(new WasteRecordedEvent(Id, TenantId, CuttingSheetId, wasteAreaCm2));
    }

    public void Fail()
    {
        if (Status != ExecutionStatus.InProgress)
            throw new InvalidOperationException($"Cannot fail execution in status {Status}.");
        Status = ExecutionStatus.Failed;
        CompletedAt = DateTime.UtcNow;
    }
}
