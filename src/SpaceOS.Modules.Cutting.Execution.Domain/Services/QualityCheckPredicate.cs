using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Domain.Services;

/// <summary>
/// Milestone is met when the offcut-to-total-area ratio is at or below the acceptable threshold.
/// </summary>
public sealed class QualityCheckPredicate : IMilestonePredicate
{
    private readonly decimal _maxOffcutRatio;

    /// <param name="maxOffcutRatio">Maximum acceptable offcut ratio (0–1). Default 0.3 = 30%.</param>
    public QualityCheckPredicate(decimal maxOffcutRatio = 0.30m)
    {
        if (maxOffcutRatio is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(maxOffcutRatio), "Ratio must be between 0 and 1.");
        _maxOffcutRatio = maxOffcutRatio;
    }

    public MilestoneKind Kind => MilestoneKind.QualityCheck;
    public int ConfigVersion => 1;

    public bool Evaluate(CuttingExecution execution, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(execution);
        if (execution.TotalAreaMm2 <= 0) return true;
        var ratio = execution.OffcutAreaMm2 / execution.TotalAreaMm2;
        return ratio <= _maxOffcutRatio;
    }
}
