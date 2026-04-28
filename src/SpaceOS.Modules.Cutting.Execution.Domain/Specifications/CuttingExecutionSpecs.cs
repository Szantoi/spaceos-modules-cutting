using Ardalis.Specification;
using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Domain.Specifications;

/// <summary>Fetches a single execution by its primary key.</summary>
public sealed class CuttingExecutionByIdSpec : Specification<CuttingExecution>
{
    public CuttingExecutionByIdSpec(Guid id)
    {
        Query.Where(e => e.Id == id);
    }
}

/// <summary>Returns all non-terminal executions for a tenant (Scheduled, Started, InProgress).</summary>
public sealed class ActiveExecutionsByTenantSpec : Specification<CuttingExecution>
{
    private static readonly CuttingExecutionStatus[] ActiveStatuses =
    [
        CuttingExecutionStatus.Scheduled,
        CuttingExecutionStatus.Started,
        CuttingExecutionStatus.InProgress
    ];

    public ActiveExecutionsByTenantSpec(Guid tenantId)
    {
        Query.Where(e => e.TenantId == tenantId && ActiveStatuses.Contains(e.Status));
    }
}

/// <summary>Returns all executions for a given sheet within a tenant.</summary>
public sealed class ExecutionsBySheetSpec : Specification<CuttingExecution>
{
    public ExecutionsBySheetSpec(Guid sheetId, Guid tenantId)
    {
        Query.Where(e => e.SheetId == sheetId && e.TenantId == tenantId);
    }
}

/// <summary>Returns executions scheduled on a specific machine and calendar date for a tenant.</summary>
public sealed class ExecutionsByMachineAndDateSpec : Specification<CuttingExecution>
{
    public ExecutionsByMachineAndDateSpec(string machineId, DateOnly date, Guid tenantId)
    {
        var startOfDay = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endOfDay = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        Query.Where(e =>
            e.TenantId == tenantId &&
            e.MachineId == machineId &&
            e.ScheduledAt >= startOfDay &&
            e.ScheduledAt <= endOfDay);
    }
}

/// <summary>Returns all executions linked to a handshake epic for a tenant.</summary>
public sealed class ExecutionsByHandshakeEpicSpec : Specification<CuttingExecution>
{
    // HandshakeEpicId is tracked via SheetId convention; extend when epic link is modelled.
    public ExecutionsByHandshakeEpicSpec(Guid epicId, Guid tenantId)
    {
        Query.Where(e => e.TenantId == tenantId && e.SheetId == epicId);
    }
}

/// <summary>Returns executions that have at least one Pending milestone for a tenant.</summary>
public sealed class PendingMilestonesSpec : Specification<CuttingExecution>
{
    public PendingMilestonesSpec(Guid tenantId)
    {
        Query.Where(e =>
            e.TenantId == tenantId &&
            e.Milestones.Any(m => m.Status == MilestoneStatus.Pending));
    }
}

/// <summary>Returns executions affected by a worker consent withdrawal, filtered by scope.</summary>
public sealed class ExecutionsByConsentScopeSpec : Specification<CuttingExecution>
{
    public ExecutionsByConsentScopeSpec(Guid workerId, ConsentScope scope, Guid tenantId)
    {
        Query.Where(e =>
            e.TenantId == tenantId &&
            e.WorkerAssignment.WorkerId == workerId &&
            (scope == ConsentScope.AllExecutions ||
             scope == ConsentScope.SpecificTenant ||
             scope == ConsentScope.SpecificExecution));
    }
}

/// <summary>Count specification: counts photo-bearing executions affected by consent withdrawal for a worker.</summary>
public sealed class ConsentAffectedPhotoCountSpec : Specification<CuttingExecution>
{
    public ConsentAffectedPhotoCountSpec(Guid workerId, Guid tenantId)
    {
        Query.Where(e =>
            e.TenantId == tenantId &&
            e.WorkerAssignment.WorkerId == workerId &&
            e.CompletionProof != null &&
            e.CompletionProof.Level == ProofLevel.PhotoEvidence);
    }
}

/// <summary>Returns the execution that owns a given execution key (by executionId + tenantId).</summary>
public sealed class ExecutionKeyByExecutionSpec : Specification<CuttingExecution>
{
    public ExecutionKeyByExecutionSpec(Guid executionId, Guid tenantId)
    {
        Query.Where(e => e.Id == executionId && e.TenantId == tenantId);
    }
}
