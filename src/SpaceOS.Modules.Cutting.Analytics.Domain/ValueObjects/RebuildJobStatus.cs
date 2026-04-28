namespace SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;

/// <summary>FSM states for an <see cref="Aggregates.AnalyticsRebuildJob"/>.</summary>
public enum RebuildJobStatus
{
    Pending,
    Running,
    Completed,
    Failed
}
