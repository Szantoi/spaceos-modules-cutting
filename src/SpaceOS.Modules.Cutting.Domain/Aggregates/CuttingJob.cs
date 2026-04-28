using SpaceOS.Modules.Cutting.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Domain.Aggregates;

/// <summary>Child entity of DaySlot representing a single order's cutting work scheduled on a day.</summary>
public class CuttingJob
{
    public Guid Id { get; private set; }
    public Guid DaySlotId { get; private set; }
    public Guid OrderId { get; private set; }
    public DateTime ScheduledDate { get; private set; }
    public string Priority { get; private set; } = "Normal";
    public decimal EstimatedTimeHours { get; private set; }
    public string Status { get; private set; } = "Pending";
    /// <summary>Part width in mm. 0 when geometry is not yet available (Phase 3+).</summary>
    public decimal WidthMm { get; private set; }
    /// <summary>Part height in mm. 0 when geometry is not yet available (Phase 3+).</summary>
    public decimal HeightMm { get; private set; }
    /// <summary>Material code (e.g. "MDF 18mm"). Empty for legacy jobs.</summary>
    public string Material { get; private set; } = string.Empty;
    /// <summary>Grain direction constraint for the part.</summary>
    public GrainDirection GrainDirection { get; private set; } = GrainDirection.None;

    private CuttingJob() { }

    /// <summary>Creates a CuttingJob. Valid priorities: Urgent, High, Normal, Low.</summary>
    public static CuttingJob Create(Guid daySlotId, Guid orderId, DateTime scheduledDate, string priority,
        decimal estimatedTimeHours, decimal widthMm = 0m, decimal heightMm = 0m,
        string material = "", GrainDirection grainDirection = GrainDirection.None)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("OrderId required.", nameof(orderId));
        if (estimatedTimeHours <= 0) throw new ArgumentException("EstimatedTimeHours must be > 0.", nameof(estimatedTimeHours));
        if (priority != "Urgent" && priority != "High" && priority != "Normal" && priority != "Low")
            throw new ArgumentException("Invalid priority.", nameof(priority));

        return new CuttingJob
        {
            Id = Guid.NewGuid(),
            DaySlotId = daySlotId,
            OrderId = orderId,
            ScheduledDate = DateTime.SpecifyKind(scheduledDate.Date, DateTimeKind.Utc),
            Priority = priority,
            EstimatedTimeHours = estimatedTimeHours,
            WidthMm = widthMm,
            HeightMm = heightMm,
            Material = material ?? string.Empty,
            GrainDirection = grainDirection,
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

    /// <summary>Moves this job to a different DaySlot (rework/reschedule).</summary>
    public void RescheduleTo(Guid newDaySlotId)
    {
        DaySlotId = newDaySlotId;
    }

    /// <summary>Marks this job as Warning — no available slot could be found during rework.</summary>
    public void MarkAsWarning()
    {
        Status = "Warning";
    }
}
