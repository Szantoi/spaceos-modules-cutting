using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Application.Entities;

public enum ConsentWithdrawalStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

/// <summary>Tracks the lifecycle of a GDPR worker consent withdrawal request.</summary>
public sealed class ConsentWithdrawal
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid WorkerId { get; private set; }
    public ConsentScope Scope { get; private set; }
    public ConsentWithdrawalStatus Status { get; private set; }
    public int ProcessedPhotos { get; private set; }
    public int FailedPhotos { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? ProcessingStartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private ConsentWithdrawal() { }

    public static ConsentWithdrawal Create(Guid tenantId, Guid workerId, ConsentScope scope, DateTime requestedAt)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));
        if (workerId == Guid.Empty) throw new ArgumentException("WorkerId required.", nameof(workerId));

        return new ConsentWithdrawal
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WorkerId = workerId,
            Scope = scope,
            Status = ConsentWithdrawalStatus.Pending,
            RequestedAt = requestedAt
        };
    }

    public void MarkProcessing(DateTime now)
    {
        Status = ConsentWithdrawalStatus.Processing;
        ProcessingStartedAt = now;
    }

    public void MarkCompleted(DateTime now)
    {
        Status = ConsentWithdrawalStatus.Completed;
        CompletedAt = now;
    }

    public void MarkFailed(DateTime now)
    {
        Status = ConsentWithdrawalStatus.Failed;
        CompletedAt = now;
    }

    public void IncrementProcessed() => ProcessedPhotos++;
    public void IncrementFailed() => FailedPhotos++;
}
