namespace SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;

/// <summary>
/// Per-machine, per-day execution summary projected from <c>CuttingJobCompleted</c> events.
/// This is a read-model entity — not an aggregate; no domain events.
/// </summary>
public sealed class DailyExecutionMetric
{
    /// <summary>Surrogate primary key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Machine identifier this metric belongs to.</summary>
    public string MachineId { get; private set; } = string.Empty;

    /// <summary>Calendar date the metric covers.</summary>
    public DateOnly MetricDate { get; private set; }

    /// <summary>Number of completed executions for the day.</summary>
    public int CompletedCount { get; private set; }

    /// <summary>Mean execution wall-clock time in minutes.</summary>
    public decimal AvgDurationMinutes { get; private set; }

    /// <summary>Material yield percentage [0–100].</summary>
    public decimal YieldPercent { get; private set; }

    /// <summary>UTC timestamp of the last projection write.</summary>
    public DateTime LastUpdatedAt { get; private set; }

    private DailyExecutionMetric() { }

    /// <summary>Creates a new <see cref="DailyExecutionMetric"/> with validated inputs.</summary>
    public static DailyExecutionMetric Create(
        Guid tenantId, string machineId, DateOnly date,
        int completedCount, decimal avgDurationMinutes, decimal yieldPercent)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(machineId)) throw new ArgumentException("MachineId required.", nameof(machineId));
        if (completedCount < 0) throw new ArgumentOutOfRangeException(nameof(completedCount));
        if (yieldPercent is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(yieldPercent));

        return new DailyExecutionMetric
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MachineId = machineId,
            MetricDate = date,
            CompletedCount = completedCount,
            AvgDurationMinutes = avgDurationMinutes,
            YieldPercent = yieldPercent,
            LastUpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Updates counters when a later event arrives for the same machine/date.</summary>
    public void Update(int completedCount, decimal avgDurationMinutes, decimal yieldPercent)
    {
        if (completedCount < 0) throw new ArgumentOutOfRangeException(nameof(completedCount));
        if (yieldPercent is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(yieldPercent));

        CompletedCount = completedCount;
        AvgDurationMinutes = avgDurationMinutes;
        YieldPercent = yieldPercent;
        LastUpdatedAt = DateTime.UtcNow;
    }
}
