using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Domain.Entities;

/// <summary>Owned entity describing a milestone subscription on an execution.</summary>
public sealed class MilestoneSubscription
{
    public Guid MilestoneId { get; private set; }
    public MilestoneKind Kind { get; private set; }
    public string ConfigJson { get; private set; } = string.Empty;
    public int ConfigVersion { get; private set; }
    public MilestoneStatus Status { get; private set; }
    public DateTime? ReachedAt { get; private set; }

    private MilestoneSubscription() { }

    internal static MilestoneSubscription Create(Guid milestoneId, MilestoneKind kind, string configJson, int configVersion)
        => new()
        {
            MilestoneId = milestoneId,
            Kind = kind,
            ConfigJson = configJson,
            ConfigVersion = configVersion,
            Status = MilestoneStatus.Pending
        };

    internal void MarkMet(DateTime now)
    {
        Status = MilestoneStatus.Met;
        ReachedAt = now;
    }

    internal void MarkExpired() => Status = MilestoneStatus.Expired;
}
