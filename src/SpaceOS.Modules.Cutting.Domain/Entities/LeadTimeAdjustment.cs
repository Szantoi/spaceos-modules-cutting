namespace SpaceOS.Modules.Cutting.Domain.Entities;

/// <summary>
/// Entity representing a lead time adjustment for dynamic pricing.
/// </summary>
public class LeadTimeAdjustment
{
    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the pricing rule identifier this adjustment belongs to.</summary>
    public Guid PricingRuleId { get; private set; }

    /// <summary>Gets the lead time in days.</summary>
    public int LeadDays { get; private set; }

    /// <summary>Gets the adjustment factor (e.g., 1.1 for 10% increase, 0.9 for 10% decrease).</summary>
    public decimal AdjustmentFactor { get; private set; }

    /// <summary>Parameterless constructor for EF Core.</summary>
    private LeadTimeAdjustment() { }

    /// <summary>
    /// Creates a new lead time adjustment.
    /// </summary>
    /// <param name="pricingRuleId">The pricing rule identifier.</param>
    /// <param name="leadDays">Number of lead days.</param>
    /// <param name="adjustmentFactor">Price adjustment factor.</param>
    /// <returns>A new lead time adjustment instance.</returns>
    internal static LeadTimeAdjustment Create(Guid pricingRuleId, int leadDays, decimal adjustmentFactor)
    {
        return new LeadTimeAdjustment
        {
            Id = Guid.NewGuid(),
            PricingRuleId = pricingRuleId,
            LeadDays = leadDays,
            AdjustmentFactor = adjustmentFactor
        };
    }
}
