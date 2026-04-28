namespace SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;

/// <summary>
/// Per-worker, per-day execution summary with SEC-06 k-anonymity suppression.
/// When the number of distinct workers in the rolling window is below the k-threshold
/// the record is suppressed: <see cref="WorkerId"/> is nulled and <see cref="IsSuppressed"/> is set.
/// Read-model entity — no domain events, no FSM.
/// </summary>
public sealed class DailyOperatorMetric
{
    /// <summary>Surrogate primary key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Worker identity. <see langword="null"/> when the record is suppressed for privacy.
    /// </summary>
    public Guid? WorkerId { get; private set; }

    /// <summary>Calendar date the metric covers.</summary>
    public DateOnly MetricDate { get; private set; }

    /// <summary>Number of completed executions attributed to this worker on the day.</summary>
    public int CompletedExecutions { get; private set; }

    /// <summary>Mean execution duration in minutes.</summary>
    public decimal AvgDurationMinutes { get; private set; }

    /// <summary>
    /// <see langword="true"/> when the worker identity was suppressed because the group size
    /// fell below the k-anonymity threshold (SEC-06).
    /// </summary>
    public bool IsSuppressed { get; private set; }

    /// <summary>UTC timestamp of the last projection write.</summary>
    public DateTime LastUpdatedAt { get; private set; }

    private DailyOperatorMetric() { }

    /// <summary>Creates a new visible (non-suppressed) <see cref="DailyOperatorMetric"/>.</summary>
    public static DailyOperatorMetric Create(
        Guid tenantId, Guid workerId, DateOnly date,
        int completedExecutions, decimal avgDurationMinutes)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));
        if (workerId == Guid.Empty) throw new ArgumentException("WorkerId required.", nameof(workerId));
        if (completedExecutions < 0) throw new ArgumentOutOfRangeException(nameof(completedExecutions));

        return new DailyOperatorMetric
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WorkerId = workerId,
            MetricDate = date,
            CompletedExecutions = completedExecutions,
            AvgDurationMinutes = avgDurationMinutes,
            IsSuppressed = false,
            LastUpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Suppresses the worker identity when the k-anonymity threshold is not met (SEC-06).
    /// </summary>
    public void Suppress()
    {
        WorkerId = null;
        IsSuppressed = true;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Updates performance counters when a later event arrives for the same worker/date.</summary>
    public void Update(int completedExecutions, decimal avgDurationMinutes)
    {
        CompletedExecutions = completedExecutions;
        AvgDurationMinutes = avgDurationMinutes;
        LastUpdatedAt = DateTime.UtcNow;
    }
}
