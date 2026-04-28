using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Domain.Services;

/// <summary>Milestone is met when all panels in the execution are completed.</summary>
public sealed class PanelCompletionPredicate : IMilestonePredicate
{
    public MilestoneKind Kind => MilestoneKind.PanelCompletion;
    public int ConfigVersion => 1;

    public bool Evaluate(CuttingExecution execution, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(execution);
        return execution.PanelsCompleted >= execution.TotalPanels && execution.TotalPanels > 0;
    }
}
