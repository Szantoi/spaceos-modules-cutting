namespace SpaceOS.Modules.Cutting.Domain.Entities;

/// <summary>
/// Entity representing a material-specific surcharge for pricing.
/// </summary>
public class MaterialSurcharge
{
    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the pricing rule identifier this surcharge belongs to.</summary>
    public Guid PricingRuleId { get; private set; }

    /// <summary>Gets the material identifier.</summary>
    public Guid MaterialId { get; private set; }

    /// <summary>Gets the surcharge percentage (0-1000).</summary>
    public decimal SurchargePercent { get; private set; }

    /// <summary>Parameterless constructor for EF Core.</summary>
    private MaterialSurcharge() { }

    /// <summary>
    /// Creates a new material surcharge.
    /// </summary>
    /// <param name="pricingRuleId">The pricing rule identifier.</param>
    /// <param name="materialId">Material identifier.</param>
    /// <param name="surchargePercent">Surcharge percentage (0-1000).</param>
    /// <returns>A new material surcharge instance.</returns>
    internal static MaterialSurcharge Create(Guid pricingRuleId, Guid materialId, decimal surchargePercent)
    {
        return new MaterialSurcharge
        {
            Id = Guid.NewGuid(),
            PricingRuleId = pricingRuleId,
            MaterialId = materialId,
            SurchargePercent = surchargePercent
        };
    }
}
