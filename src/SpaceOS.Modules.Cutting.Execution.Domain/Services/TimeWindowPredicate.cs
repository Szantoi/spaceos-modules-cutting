using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Domain.Services;

/// <summary>Milestone is met when <paramref name="now"/> falls within the execution's schedule window.</summary>
public sealed class TimeWindowPredicate : IMilestonePredicate
{
    public MilestoneKind Kind => MilestoneKind.TimeWindow;
    public int ConfigVersion => 1;

    public bool Evaluate(CuttingExecution execution, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(execution);
        return execution.ScheduleWindow.Contains(now);
    }
}
