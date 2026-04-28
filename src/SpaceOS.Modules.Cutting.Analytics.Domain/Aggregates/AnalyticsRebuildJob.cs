using Ardalis.Result;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Analytics.Domain.Aggregates;

/// <summary>
/// Tracks a tenant-scoped full rebuild of analytics read-models.
/// FSM: Pending → Running → Completed | Failed.
/// </summary>
public sealed class AnalyticsRebuildJob
{
    /// <summary>Surrogate primary key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Tenant this rebuild is scoped to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Current FSM state.</summary>
    public RebuildJobStatus Status { get; private set; }

    /// <summary>UTC time the job was requested.</summary>
    public DateTime RequestedAt { get; private set; }

    /// <summary>UTC time the job transitioned to Running; null until then.</summary>
    public DateTime? StartedAt { get; private set; }

    /// <summary>UTC time the job reached a terminal state; null until then.</summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>Error description when <see cref="Status"/> is Failed.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Number of event chunks processed so far.</summary>
    public int ProcessedChunks { get; private set; }

    /// <summary>Total number of event chunks to process.</summary>
    public int TotalChunks { get; private set; }

    private AnalyticsRebuildJob() { }

    /// <summary>Creates a new <see cref="AnalyticsRebuildJob"/> in the Pending state.</summary>
    public static AnalyticsRebuildJob Create(Guid tenantId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));

        return new AnalyticsRebuildJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Status = RebuildJobStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };
    }

    /// <summary>Transitions from Pending to Running and records the total chunk count.</summary>
    public Result Start(int totalChunks)
    {
        if (Status != RebuildJobStatus.Pending)
            return Result.Invalid(new ValidationError($"Cannot start job in status {Status}."));
        if (totalChunks < 0)
            return Result.Invalid(new ValidationError("TotalChunks cannot be negative."));

        Status = RebuildJobStatus.Running;
        StartedAt = DateTime.UtcNow;
        TotalChunks = totalChunks;
        return Result.Success();
    }

    /// <summary>Increments <see cref="ProcessedChunks"/> while the job is Running.</summary>
    public Result RecordChunkProgress()
    {
        if (Status != RebuildJobStatus.Running)
            return Result.Invalid(new ValidationError($"Cannot record progress in status {Status}."));

        ProcessedChunks++;
        return Result.Success();
    }

    /// <summary>Transitions from Running to Completed.</summary>
    public Result Complete()
    {
        if (Status != RebuildJobStatus.Running)
            return Result.Invalid(new ValidationError($"Cannot complete job in status {Status}."));

        Status = RebuildJobStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>Transitions to Failed from any non-Completed state.</summary>
    public Result Fail(string errorMessage)
    {
        if (Status == RebuildJobStatus.Completed)
            return Result.Invalid(new ValidationError("Cannot fail a completed job."));

        Status = RebuildJobStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
