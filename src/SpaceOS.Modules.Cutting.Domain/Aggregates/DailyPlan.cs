namespace SpaceOS.Modules.Cutting.Domain.Aggregates;

/// <summary>Child entity of CuttingPlan representing one day's scheduled cutting work.</summary>
public class DailyPlan
{
    public Guid Id { get; private set; }
    public Guid CuttingPlanId { get; private set; }
    public DateTime Date { get; private set; }
    public decimal AvailableCapacity { get; private set; }

    private readonly List<CuttingJob> _jobs = new();
    public IReadOnlyList<CuttingJob> Jobs => _jobs.AsReadOnly();

    private DailyPlan() { }

    /// <summary>Creates a DailyPlan for the given date with the default 8-hour machine capacity.</summary>
    public static DailyPlan Create(Guid cuttingPlanId, DateTime date)
    {
        return new DailyPlan
        {
            Id = Guid.NewGuid(),
            CuttingPlanId = cuttingPlanId,
            Date = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc),
            AvailableCapacity = 8m
        };
    }

    /// <summary>Total hours allocated across all scheduled jobs.</summary>
    public decimal AllocatedCapacity => _jobs.Sum(j => j.EstimatedTimeHours);

    /// <summary>Percentage of available capacity consumed by scheduled jobs.</summary>
    public decimal UtilizationPercent => AvailableCapacity > 0 ? AllocatedCapacity / AvailableCapacity * 100 : 0;

    /// <summary>Adds a job to this daily plan.</summary>
    public void AddJob(CuttingJob job)
    {
        // DailyPlanId on the job must reference this plan
        if (job.DailyPlanId != Id) throw new InvalidOperationException("Job belongs to a different daily plan.");
        _jobs.Add(job);
    }
}
