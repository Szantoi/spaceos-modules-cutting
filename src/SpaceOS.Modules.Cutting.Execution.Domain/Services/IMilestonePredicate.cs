using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Domain.Services;

/// <summary>Evaluates whether a milestone condition is met for a given execution snapshot.</summary>
public interface IMilestonePredicate
{
    MilestoneKind Kind { get; }
    int ConfigVersion { get; }
    bool Evaluate(CuttingExecution execution, DateTime now);
}
