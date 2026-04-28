using Ardalis.Result;

namespace SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;

/// <summary>Binds a worker identity to an enrollment record.</summary>
public sealed record WorkerAssignment
{
    public Guid WorkerId { get; }
    public Guid EnrollmentId { get; }

    private WorkerAssignment(Guid workerId, Guid enrollmentId)
    {
        WorkerId = workerId;
        EnrollmentId = enrollmentId;
    }

    /// <summary>Creates a WorkerAssignment, rejecting empty GUIDs.</summary>
    public static Result<WorkerAssignment> Create(Guid workerId, Guid enrollmentId)
    {
        if (workerId == Guid.Empty)
            return Result<WorkerAssignment>.Invalid(new ValidationError("WorkerId must not be empty."));
        if (enrollmentId == Guid.Empty)
            return Result<WorkerAssignment>.Invalid(new ValidationError("EnrollmentId must not be empty."));
        return Result<WorkerAssignment>.Success(new WorkerAssignment(workerId, enrollmentId));
    }
}
