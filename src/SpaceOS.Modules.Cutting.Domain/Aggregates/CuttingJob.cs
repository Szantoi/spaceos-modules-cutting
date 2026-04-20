namespace SpaceOS.Modules.Cutting.Domain.Aggregates;

/// <summary>Child entity of DailyPlan representing a single order's cutting work scheduled on a day.</summary>
public class CuttingJob
{
    public Guid Id { get; private set; }
    public Guid DailyPlanId { get; private set; }
    public Guid OrderId { get; private set; }
    public DateTime ScheduledDate { get; private set; }
    public string Priority { get; private set; } = "Normal";
    public decimal EstimatedTimeHours { get; private set; }
    public string Status { get; private set; } = "Pending";

    private CuttingJob() { }

    /// <summary>Creates a CuttingJob. Valid priorities: Urgent, High, Normal, Low.</summary>
    public static CuttingJob Create(Guid dailyPlanId, Guid orderId, DateTime scheduledDate, string priority, decimal estimatedTimeHours)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("OrderId required.", nameof(orderId));
        if (estimatedTimeHours <= 0) throw new ArgumentException("EstimatedTimeHours must be > 0.", nameof(estimatedTimeHours));
        if (priority != "Urgent" && priority != "High" && priority != "Normal" && priority != "Low")
            throw new ArgumentException("Invalid priority.", nameof(priority));

        return new CuttingJob
        {
            Id = Guid.NewGuid(),
            DailyPlanId = dailyPlanId,
            OrderId = orderId,
            ScheduledDate = DateTime.SpecifyKind(scheduledDate.Date, DateTimeKind.Utc),
            Priority = priority,
            EstimatedTimeHours = estimatedTimeHours,
            Status = "Pending"
        };
    }

    /// <summary>
    /// Transitions status from Pending/InProgress → Cut.
    /// Called when the physical cutting of this job is confirmed complete.
    /// </summary>
    public void MarkAsCut()
    {
        if (Status == "Cut")
            throw new InvalidOperationException($"CuttingJob {Id} is already in status 'Cut'.");
        if (Status != "Pending" && Status != "InProgress")
            throw new InvalidOperationException($"CuttingJob {Id} cannot transition to 'Cut' from '{Status}'.");

        Status = "Cut";
    }
}
