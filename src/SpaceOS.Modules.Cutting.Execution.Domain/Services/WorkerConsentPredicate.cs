using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Domain.Services;

/// <summary>Milestone is met when the worker consent is active (not withdrawn).</summary>
public sealed class WorkerConsentPredicate : IMilestonePredicate
{
    public MilestoneKind Kind => MilestoneKind.WorkerConsent;
    public int ConfigVersion => 1;

    public bool Evaluate(CuttingExecution execution, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(execution);
        return execution.WorkerConsentActive;
    }
}
