using SpaceOS.Modules.Cutting.Domain.Common;
using SpaceOS.Modules.Cutting.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Domain.Aggregates;

/// <summary>
/// Aggregate root that defines which capacity model, rework policy and planning strategy
/// a tenant (or the system) applies for cutting plans.
/// TenantId = null → global preset visible to all tenants.
/// </summary>
public sealed class PriorityProfile : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid? TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }
    public string CapacityModelId { get; private set; } = string.Empty;
    public string ReworkPolicyId { get; private set; } = string.Empty;
    public string PlanningStrategyId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private readonly List<PriorityRule> _rules = new();
    public IReadOnlyList<PriorityRule> Rules => _rules.AsReadOnly();

    private PriorityProfile() { }

    public static PriorityProfile Create(
        Guid? tenantId,
        string name,
        string capacityModelId,
        string reworkPolicyId,
        string planningStrategyId,
        bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name required.", nameof(name));
        if (string.IsNullOrWhiteSpace(capacityModelId))
            throw new ArgumentException("CapacityModelId required.", nameof(capacityModelId));
        if (string.IsNullOrWhiteSpace(reworkPolicyId))
            throw new ArgumentException("ReworkPolicyId required.", nameof(reworkPolicyId));
        if (string.IsNullOrWhiteSpace(planningStrategyId))
            throw new ArgumentException("PlanningStrategyId required.", nameof(planningStrategyId));

        return new PriorityProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            IsDefault = isDefault,
            CapacityModelId = capacityModelId,
            ReworkPolicyId = reworkPolicyId,
            PlanningStrategyId = planningStrategyId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetDefault(bool value) => IsDefault = value;

    public void AddRule(PriorityRule rule) => _rules.Add(rule);
}
