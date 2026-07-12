namespace SpaceOS.Modules.Cutting.Domain.ValueObjects;

/// <summary>
/// FSM states for PricingRule lifecycle.
/// </summary>
public enum PricingRuleStatus
{
    /// <summary>Draft state - rule is being configured, not yet validated.</summary>
    Draft,

    /// <summary>Active state - rule is validated and can be used for price calculations.</summary>
    Active,

    /// <summary>Archived state - rule is no longer in use.</summary>
    Archived
}
