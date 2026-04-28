using Ardalis.Result;

namespace SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;

/// <summary>Represents the time window in which an execution is scheduled.</summary>
public sealed record ScheduleWindow
{
    public DateTime Start { get; }
    public DateTime End { get; }

    private ScheduleWindow(DateTime start, DateTime end)
    {
        Start = start;
        End = end;
    }

    /// <summary>Creates a ScheduleWindow, validating that end is strictly after start.</summary>
    public static Result<ScheduleWindow> Create(DateTime start, DateTime end)
    {
        if (end <= start)
            return Result<ScheduleWindow>.Invalid(new ValidationError("End must be after start."));
        return Result<ScheduleWindow>.Success(new ScheduleWindow(start, end));
    }

    public bool Contains(DateTime moment) => moment >= Start && moment <= End;
}
