using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Domain.Aggregates;

/// <summary>
/// Aggregate root representing supplier-specific pricing logic with FSM state management.
/// Q3 Track B Phase 1 — Pricing Rule Engine.
/// </summary>
public class PricingRule
{
    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the supplier identifier (from Procurement module).</summary>
    public Guid SupplierId { get; private set; }

    /// <summary>Gets the product category (e.g., "door", "cabinet", "panel").</summary>
    public string ProductCategory { get; private set; } = string.Empty;

    /// <summary>Gets the base price per unit.</summary>
    public decimal BasePricePerUnit { get; private set; }

    /// <summary>Gets the pricing rule status (FSM state).</summary>
    public PricingRuleStatus Status { get; private set; }

    /// <summary>Gets the creation timestamp.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Gets the last update timestamp.</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Gets the version for optimistic concurrency control.</summary>
    public int Version { get; private set; }

    private readonly List<QuantityBreakpoint> _quantityBreakpoints = new();
    private readonly List<LeadTimeAdjustment> _leadTimeAdjustments = new();
    private readonly List<MaterialSurcharge> _materialSurcharges = new();

    /// <summary>Gets the quantity breakpoints (FSM states for pricing tiers).</summary>
    public IReadOnlyList<QuantityBreakpoint> QuantityBreakpoints => _quantityBreakpoints.AsReadOnly();

    /// <summary>Gets the lead time adjustments.</summary>
    public IReadOnlyList<LeadTimeAdjustment> LeadTimeAdjustments => _leadTimeAdjustments.AsReadOnly();

    /// <summary>Gets the material surcharges.</summary>
    public IReadOnlyList<MaterialSurcharge> MaterialSurcharges => _materialSurcharges.AsReadOnly();

    /// <summary>Parameterless constructor for EF Core.</summary>
    private PricingRule() { }

    /// <summary>
    /// Creates a new pricing rule in draft state.
    /// </summary>
    /// <param name="supplierId">The supplier identifier.</param>
    /// <param name="productCategory">The product category.</param>
    /// <param name="basePricePerUnit">The base price per unit.</param>
    /// <returns>A new pricing rule instance in draft state.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public static PricingRule Create(Guid supplierId, string productCategory, decimal basePricePerUnit)
    {
        if (supplierId == Guid.Empty)
            throw new ArgumentException("SupplierId cannot be empty.", nameof(supplierId));

        if (string.IsNullOrWhiteSpace(productCategory))
            throw new ArgumentException("ProductCategory cannot be null or empty.", nameof(productCategory));

        if (basePricePerUnit <= 0)
            throw new ArgumentException("BasePricePerUnit must be greater than zero.", nameof(basePricePerUnit));

        var now = DateTime.UtcNow;
        return new PricingRule
        {
            Id = Guid.NewGuid(),
            SupplierId = supplierId,
            ProductCategory = productCategory,
            BasePricePerUnit = basePricePerUnit,
            Status = PricingRuleStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1
        };
    }

    /// <summary>
    /// Adds a quantity breakpoint to this pricing rule.
    /// </summary>
    /// <param name="minQuantity">Minimum quantity (inclusive).</param>
    /// <param name="maxQuantity">Maximum quantity (exclusive).</param>
    /// <param name="discountPercent">Discount percentage (0-100).</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when breakpoint range overlaps with existing breakpoints.</exception>
    public void AddQuantityBreakpoint(int minQuantity, int maxQuantity, decimal discountPercent)
    {
        if (minQuantity < 1)
            throw new ArgumentException("MinQuantity must be at least 1.", nameof(minQuantity));

        if (maxQuantity <= minQuantity)
            throw new ArgumentException("MaxQuantity must be greater than MinQuantity.", nameof(maxQuantity));

        if (discountPercent < 0 || discountPercent > 100)
            throw new ArgumentException("DiscountPercent must be between 0 and 100.", nameof(discountPercent));

        // Check for overlapping ranges
        if (_quantityBreakpoints.Any(bp => RangesOverlap(minQuantity, maxQuantity, bp.MinQuantity, bp.MaxQuantity)))
            throw new InvalidOperationException($"Breakpoint range [{minQuantity}, {maxQuantity}) overlaps with existing breakpoint.");

        var breakpoint = QuantityBreakpoint.Create(Id, minQuantity, maxQuantity, discountPercent);
        _quantityBreakpoints.Add(breakpoint);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a lead time adjustment to this pricing rule.
    /// </summary>
    /// <param name="leadDays">Number of lead days.</param>
    /// <param name="adjustmentFactor">Price adjustment factor (e.g., 1.1 for 10% increase, 0.9 for 10% decrease).</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public void AddLeadTimeAdjustment(int leadDays, decimal adjustmentFactor)
    {
        if (leadDays < 0)
            throw new ArgumentException("LeadDays cannot be negative.", nameof(leadDays));

        if (adjustmentFactor <= 0)
            throw new ArgumentException("AdjustmentFactor must be greater than zero.", nameof(adjustmentFactor));

        if (_leadTimeAdjustments.Any(lt => lt.LeadDays == leadDays))
            throw new InvalidOperationException($"Lead time adjustment for {leadDays} days already exists.");

        var adjustment = LeadTimeAdjustment.Create(Id, leadDays, adjustmentFactor);
        _leadTimeAdjustments.Add(adjustment);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a material surcharge to this pricing rule.
    /// </summary>
    /// <param name="materialId">Material identifier.</param>
    /// <param name="surchargePercent">Surcharge percentage (0-1000).</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public void AddMaterialSurcharge(Guid materialId, decimal surchargePercent)
    {
        if (materialId == Guid.Empty)
            throw new ArgumentException("MaterialId cannot be empty.", nameof(materialId));

        if (surchargePercent < 0 || surchargePercent > 1000)
            throw new ArgumentException("SurchargePercent must be between 0 and 1000.", nameof(surchargePercent));

        if (_materialSurcharges.Any(ms => ms.MaterialId == materialId))
            throw new InvalidOperationException($"Material surcharge for material {materialId} already exists.");

        var surcharge = MaterialSurcharge.Create(Id, materialId, surchargePercent);
        _materialSurcharges.Add(surcharge);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates this pricing rule (FSM transition: draft → active).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when FSM transition is invalid.</exception>
    public void Activate()
    {
        if (Status == PricingRuleStatus.Archived)
            throw new InvalidOperationException("Cannot activate archived pricing rule.");

        if (Status == PricingRuleStatus.Active)
            return; // Already active, no-op

        // Validation: must have at least one quantity breakpoint
        if (!_quantityBreakpoints.Any())
            throw new InvalidOperationException("Cannot activate pricing rule without at least one quantity breakpoint.");

        // Validation: basePricePerUnit must be > 0
        if (BasePricePerUnit <= 0)
            throw new InvalidOperationException("Cannot activate pricing rule with invalid base price.");

        Status = PricingRuleStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    /// <summary>
    /// Archives this pricing rule (FSM transition: any → archived).
    /// </summary>
    public void Archive()
    {
        Status = PricingRuleStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    /// <summary>
    /// Calculates the price for a given quantity, lead time, and material.
    /// </summary>
    /// <param name="quantity">Quantity of units.</param>
    /// <param name="leadDays">Lead time in days.</param>
    /// <param name="materialId">Material identifier (optional).</param>
    /// <returns>Price calculation result with breakdown.</returns>
    /// <exception cref="InvalidOperationException">Thrown when pricing rule is not active.</exception>
    public PriceCalculationResult CalculatePrice(int quantity, int leadDays, Guid? materialId = null)
    {
        if (Status != PricingRuleStatus.Active)
            throw new InvalidOperationException("Cannot calculate price for non-active pricing rule.");

        if (quantity < 1)
            throw new ArgumentException("Quantity must be at least 1.", nameof(quantity));

        if (leadDays < 0)
            throw new ArgumentException("LeadDays cannot be negative.", nameof(leadDays));

        // Base price
        decimal price = BasePricePerUnit;
        var breakdown = new List<string> { $"Base: {BasePricePerUnit:F2} HUF" };

        // Apply quantity breakpoint discount
        var breakpoint = _quantityBreakpoints
            .FirstOrDefault(bp => quantity >= bp.MinQuantity && quantity < bp.MaxQuantity);

        if (breakpoint != null)
        {
            decimal discountFactor = 1 - (breakpoint.DiscountPercent / 100m);
            price *= discountFactor;
            breakdown.Add($"Qty Breakpoint ({breakpoint.MinQuantity}-{breakpoint.MaxQuantity}): ×{discountFactor:F2} ({breakpoint.DiscountPercent}% discount)");
        }
        else
        {
            breakdown.Add($"Qty Breakpoint: None (quantity {quantity} not in any range)");
        }

        // Apply lead time adjustment
        var leadTimeAdj = _leadTimeAdjustments
            .Where(lt => leadDays >= lt.LeadDays)
            .OrderByDescending(lt => lt.LeadDays)
            .FirstOrDefault();

        if (leadTimeAdj != null)
        {
            price *= leadTimeAdj.AdjustmentFactor;
            breakdown.Add($"LeadTime Adj ({leadTimeAdj.LeadDays}+ days): ×{leadTimeAdj.AdjustmentFactor:F2}");
        }
        else
        {
            breakdown.Add($"LeadTime Adj: None (lead time {leadDays} days)");
        }

        // Apply material surcharge
        if (materialId.HasValue && materialId.Value != Guid.Empty)
        {
            var surcharge = _materialSurcharges.FirstOrDefault(ms => ms.MaterialId == materialId.Value);
            if (surcharge != null)
            {
                decimal surchargeFactor = 1 + (surcharge.SurchargePercent / 100m);
                price *= surchargeFactor;
                breakdown.Add($"Material Surcharge: ×{surchargeFactor:F2} ({surcharge.SurchargePercent}% surcharge)");
            }
            else
            {
                breakdown.Add($"Material Surcharge: None (material {materialId} not found)");
            }
        }

        return new PriceCalculationResult(price, string.Join(" → ", breakdown));
    }

    /// <summary>
    /// Checks if two numeric ranges overlap.
    /// </summary>
    private static bool RangesOverlap(int min1, int max1, int min2, int max2)
    {
        return min1 < max2 && min2 < max1;
    }
}
