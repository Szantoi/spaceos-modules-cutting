namespace SpaceOS.Modules.Cutting.Domain.Entities;

/// <summary>
/// Entity representing a quantity breakpoint for pricing tiers (FSM state transitions).
/// </summary>
public class QuantityBreakpoint
{
    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the pricing rule identifier this breakpoint belongs to.</summary>
    public Guid PricingRuleId { get; private set; }

    /// <summary>Gets the minimum quantity (inclusive).</summary>
    public int MinQuantity { get; private set; }

    /// <summary>Gets the maximum quantity (exclusive).</summary>
    public int MaxQuantity { get; private set; }

    /// <summary>Gets the discount percentage (0-100).</summary>
    public decimal DiscountPercent { get; private set; }

    /// <summary>Parameterless constructor for EF Core.</summary>
    private QuantityBreakpoint() { }

    /// <summary>
    /// Creates a new quantity breakpoint.
    /// </summary>
    /// <param name="pricingRuleId">The pricing rule identifier.</param>
    /// <param name="minQuantity">Minimum quantity (inclusive).</param>
    /// <param name="maxQuantity">Maximum quantity (exclusive).</param>
    /// <param name="discountPercent">Discount percentage (0-100).</param>
    /// <returns>A new quantity breakpoint instance.</returns>
    internal static QuantityBreakpoint Create(Guid pricingRuleId, int minQuantity, int maxQuantity, decimal discountPercent)
    {
        return new QuantityBreakpoint
        {
            Id = Guid.NewGuid(),
            PricingRuleId = pricingRuleId,
            MinQuantity = minQuantity,
            MaxQuantity = maxQuantity,
            DiscountPercent = discountPercent
        };
    }
}
