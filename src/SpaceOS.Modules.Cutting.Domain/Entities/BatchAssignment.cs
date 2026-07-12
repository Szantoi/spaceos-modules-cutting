using Ardalis.Result;

namespace SpaceOS.Modules.Cutting.Domain.Entities;

/// <summary>
/// Records the assignment of a CuttingBatch to a machine and operator for execution.
/// Enforces idempotency via unique constraint on (BatchId, PlanDate).
/// </summary>
public sealed class BatchAssignment
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid BatchId { get; private set; }
    public DateOnly PlanDate { get; private set; }
    public Guid MachineId { get; private set; }
    public Guid OperatorId { get; private set; }
    public Guid ExecutionId { get; private set; }
    public int Priority { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private BatchAssignment() { }

    /// <summary>Creates a new BatchAssignment with validation.</summary>
    public static Result<BatchAssignment> Create(
        Guid tenantId,
        Guid batchId,
        DateOnly planDate,
        Guid machineId,
        Guid operatorId,
        Guid executionId,
        int priority,
        DateTime startTime)
    {
        if (tenantId == Guid.Empty)
            return Result<BatchAssignment>.Invalid(new ValidationError("TenantId must not be empty."));
        if (batchId == Guid.Empty)
            return Result<BatchAssignment>.Invalid(new ValidationError("BatchId must not be empty."));
        if (machineId == Guid.Empty)
            return Result<BatchAssignment>.Invalid(new ValidationError("MachineId must not be empty."));
        if (operatorId == Guid.Empty)
            return Result<BatchAssignment>.Invalid(new ValidationError("OperatorId must not be empty."));
        if (executionId == Guid.Empty)
            return Result<BatchAssignment>.Invalid(new ValidationError("ExecutionId must not be empty."));
        if (priority < 1 || priority > 10)
            return Result<BatchAssignment>.Invalid(new ValidationError("Priority must be between 1 and 10."));
        if (startTime < DateTime.UtcNow.AddMinutes(-5))
            return Result<BatchAssignment>.Invalid(new ValidationError("StartTime cannot be in the past."));

        var assignment = new BatchAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BatchId = batchId,
            PlanDate = planDate,
            MachineId = machineId,
            OperatorId = operatorId,
            ExecutionId = executionId,
            Priority = priority,
            StartTime = DateTime.SpecifyKind(startTime, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow
        };

        return Result<BatchAssignment>.Success(assignment);
    }
}
