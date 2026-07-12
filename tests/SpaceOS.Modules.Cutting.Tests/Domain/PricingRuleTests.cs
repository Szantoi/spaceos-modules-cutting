using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Domain;

/// <summary>
/// Unit tests for PricingRule aggregate (MSG-BACKEND-031).
/// </summary>
public class PricingRuleTests
{
    [Fact]
    public void Create_ValidParameters_CreatesDraftPricingRule()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var productCategory = "Wood Panels";
        var basePricePerUnit = 100m;

        // Act
        var pricingRule = PricingRule.Create(supplierId, productCategory, basePricePerUnit);

        // Assert
        Assert.NotEqual(Guid.Empty, pricingRule.Id);
        Assert.Equal(supplierId, pricingRule.SupplierId);
        Assert.Equal(productCategory, pricingRule.ProductCategory);
        Assert.Equal(basePricePerUnit, pricingRule.BasePricePerUnit);
        Assert.Equal(PricingRuleStatus.Draft, pricingRule.Status);
        Assert.Equal(1, pricingRule.Version);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_InvalidBasePricePerUnit_ThrowsArgumentException(decimal invalidPrice)
    {
        // Arrange
        var supplierId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            PricingRule.Create(supplierId, "Wood Panels", invalidPrice));
    }

    [Fact]
    public void Create_EmptyProductCategory_ThrowsArgumentException()
    {
        // Arrange
        var supplierId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            PricingRule.Create(supplierId, "", 100m));
    }

    [Fact]
    public void AddQuantityBreakpoint_ValidBreakpoint_AddsToCollection()
    {
        // Arrange
        var pricingRule = PricingRule.Create(Guid.NewGuid(), "Wood Panels", 100m);

        // Act
        pricingRule.AddQuantityBreakpoint(1, 10, 0m);
        pricingRule.AddQuantityBreakpoint(11, 50, 5m);

        // Assert
        Assert.Equal(2, pricingRule.QuantityBreakpoints.Count);
        Assert.Contains(pricingRule.QuantityBreakpoints, b => b.MinQuantity == 1 && b.MaxQuantity == 10);
        Assert.Contains(pricingRule.QuantityBreakpoints, b => b.MinQuantity == 11 && b.MaxQuantity == 50);
    }

    [Fact]
    public void AddLeadTimeAdjustment_ValidAdjustment_AddsToCollection()
    {
        // Arrange
        var pricingRule = PricingRule.Create(Guid.NewGuid(), "Wood Panels", 100m);

        // Act
        pricingRule.AddLeadTimeAdjustment(3, 0.8m);   // 3 days → 20% discount
        pricingRule.AddLeadTimeAdjustment(7, 0.7m);   // 7 days → 30% discount

        // Assert
        Assert.Equal(2, pricingRule.LeadTimeAdjustments.Count);
        Assert.Contains(pricingRule.LeadTimeAdjustments, a => a.LeadDays == 3 && a.AdjustmentFactor == 0.8m);
    }

    [Fact]
    public void AddMaterialSurcharge_ValidSurcharge_AddsToCollection()
    {
        // Arrange
        var pricingRule = PricingRule.Create(Guid.NewGuid(), "Wood Panels", 100m);
        var materialId = Guid.NewGuid();

        // Act
        pricingRule.AddMaterialSurcharge(materialId, 15m);  // 15% surcharge

        // Assert
        Assert.Single(pricingRule.MaterialSurcharges);
        Assert.Contains(pricingRule.MaterialSurcharges, s => s.MaterialId == materialId && s.SurchargePercent == 15m);
    }

    [Fact]
    public void Activate_WithBreakpoints_TransitionsToDraftToActive()
    {
        // Arrange
        var pricingRule = PricingRule.Create(Guid.NewGuid(), "Wood Panels", 100m);
        pricingRule.AddQuantityBreakpoint(1, 10, 0m);
        var initialVersion = pricingRule.Version;

        // Act
        pricingRule.Activate();

        // Assert
        Assert.Equal(PricingRuleStatus.Active, pricingRule.Status);
        Assert.Equal(initialVersion + 1, pricingRule.Version);
    }

    [Fact]
    public void Activate_WithoutBreakpoints_ThrowsInvalidOperationException()
    {
        // Arrange
        var pricingRule = PricingRule.Create(Guid.NewGuid(), "Wood Panels", 100m);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => pricingRule.Activate());
    }

    [Fact]
    public void Activate_AlreadyActive_IsIdempotent()
    {
        // Arrange
        var pricingRule = PricingRule.Create(Guid.NewGuid(), "Wood Panels", 100m);
        pricingRule.AddQuantityBreakpoint(1, 10, 0m);
        pricingRule.Activate();
        var versionAfterFirstActivation = pricingRule.Version;

        // Act
        pricingRule.Activate();

        // Assert
        Assert.Equal(PricingRuleStatus.Active, pricingRule.Status);
        Assert.Equal(versionAfterFirstActivation, pricingRule.Version);  // Version does NOT increment (no-op)
    }

    [Fact]
    public void Archive_ActiveRule_TransitionsToArchived()
    {
        // Arrange
        var pricingRule = PricingRule.Create(Guid.NewGuid(), "Wood Panels", 100m);
        pricingRule.AddQuantityBreakpoint(1, 10, 0m);
        pricingRule.Activate();

        // Act
        pricingRule.Archive();

        // Assert
        Assert.Equal(PricingRuleStatus.Archived, pricingRule.Status);
    }

    [Fact]
    public void Activate_ArchivedRule_ThrowsInvalidOperationException()
    {
        // Arrange
        var pricingRule = PricingRule.Create(Guid.NewGuid(), "Wood Panels", 100m);
        pricingRule.AddQuantityBreakpoint(1, 10, 0m);
        pricingRule.Activate();
        pricingRule.Archive();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => pricingRule.Activate());
    }

    [Fact]
    public void CalculatePrice_SingleBreakpoint_NoAdjustments_ReturnsBasePrice()
    {
        // Arrange
        var pricingRule = PricingRule.Create(Guid.NewGuid(), "Wood Panels", 100m);
        pricingRule.AddQuantityBreakpoint(1, 101, 0m);  // 0% discount (MaxQuantity is exclusive)
        pricingRule.Activate();

        // Act
        var result = pricingRule.CalculatePrice(5, 0);

        // Assert
        Assert.Equal(100m, result.Price);
        Assert.Contains("Base: 100.00 HUF", result.Breakdown);
        Assert.Contains("Qty Breakpoint (1-101):", result.Breakdown);
    }

    [Fact]
    public void CalculatePrice_QuantityBreakpoint_AppliesDiscount()
    {
        // Arrange
        var pricingRule = PricingRule.Create(Guid.NewGuid(), "Wood Panels", 100m);
        pricingRule.AddQuantityBreakpoint(1, 11, 0m);
        pricingRule.AddQuantityBreakpoint(11, 51, 10m);   // 10% discount for 11-50 units (MaxQuantity=51 is exclusive)
        pricingRule.Activate();

        // Act
        var result = pricingRule.CalculatePrice(20, 0);

        // Assert
        Assert.Equal(90m, result.Price);  // 100 * 0.9 = 90 (10% discount)
        Assert.Contains("10% discount", result.Breakdown);
    }

    [Fact]
    public void CalculatePrice_LeadTimeAdjustment_AppliesMultiplier()
    {
        // Arrange
        var pricingRule = PricingRule.Create(Guid.NewGuid(), "Wood Panels", 100m);
        pricingRule.AddQuantityBreakpoint(1, 101, 0m);
        pricingRule.AddLeadTimeAdjustment(7, 0.8m);  // 7+ days → 0.8x multiplier (20% discount)
        pricingRule.Activate();

        // Act
        var result = pricingRule.CalculatePrice(5, 7);

        // Assert
        Assert.Equal(80m, result.Price);  // 100 * 0.8 = 80
        Assert.Contains("LeadTime Adj (7+ days): ×0.80", result.Breakdown);
    }

    [Fact]
    public void CalculatePrice_MaterialSurcharge_AppliesPercentage()
    {
        // Arrange
        var materialId = Guid.NewGuid();
        var pricingRule = PricingRule.Create(Guid.NewGuid(), "Wood Panels", 100m);
        pricingRule.AddQuantityBreakpoint(1, 101, 0m);
        pricingRule.AddMaterialSurcharge(materialId, 20m);  // 20% surcharge
        pricingRule.Activate();

        // Act
        var result = pricingRule.CalculatePrice(5, 0, materialId);

        // Assert
        Assert.Equal(120m, result.Price);  // 100 * 1.2 = 120
        Assert.Contains("Material Surcharge: ×1.20 (20% surcharge)", result.Breakdown);
    }

    [Fact]
    public void CalculatePrice_AllAdjustments_CombinesCorrectly()
    {
        // Arrange
        var materialId = Guid.NewGuid();
        var pricingRule = PricingRule.Create(Guid.NewGuid(), "Wood Panels", 100m);
        pricingRule.AddQuantityBreakpoint(1, 11, 0m);
        pricingRule.AddQuantityBreakpoint(11, 51, 10m);   // 10% discount for 11-50 units
        pricingRule.AddLeadTimeAdjustment(7, 0.9m);       // 0.9x multiplier for 7+ days
        pricingRule.AddMaterialSurcharge(materialId, 15m); // 15% surcharge
        pricingRule.Activate();

        // Act
        var result = pricingRule.CalculatePrice(20, 7, materialId);

        // Assert
        // Calculation: 100 → apply 10% discount (×0.9) = 90 → apply lead time (×0.9) = 81 → apply 15% surcharge (×1.15) = 93.15
        Assert.Equal(93.15m, result.Price);
        Assert.Contains("Base: 100.00 HUF", result.Breakdown);
        Assert.Contains("10% discount", result.Breakdown);
        Assert.Contains("×0.90", result.Breakdown);
        Assert.Contains("15% surcharge", result.Breakdown);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CalculatePrice_InvalidQuantity_ThrowsArgumentException(int invalidQuantity)
    {
        // Arrange
        var pricingRule = PricingRule.Create(Guid.NewGuid(), "Wood Panels", 100m);
        pricingRule.AddQuantityBreakpoint(1, 100, 0m);
        pricingRule.Activate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => pricingRule.CalculatePrice(invalidQuantity, 0));
    }

    [Fact]
    public void CalculatePrice_NegativeLeadDays_ThrowsArgumentException()
    {
        // Arrange
        var pricingRule = PricingRule.Create(Guid.NewGuid(), "Wood Panels", 100m);
        pricingRule.AddQuantityBreakpoint(1, 100, 0m);
        pricingRule.Activate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => pricingRule.CalculatePrice(5, -1));
    }

    [Fact]
    public void CalculatePrice_QuantityNotInAnyBreakpoint_UsesBasePriceWithNote()
    {
        // Arrange
        var pricingRule = PricingRule.Create(Guid.NewGuid(), "Wood Panels", 100m);
        pricingRule.AddQuantityBreakpoint(1, 11, 0m);   // 1-10 units
        pricingRule.AddQuantityBreakpoint(20, 51, 10m);  // 20-50 units
        pricingRule.Activate();

        // Act (11-19 is not covered)
        var result = pricingRule.CalculatePrice(15, 0);

        // Assert - Uses base price and adds a note about missing breakpoint
        Assert.Equal(100m, result.Price);  // No discount applied
        Assert.Contains("Qty Breakpoint: None (quantity 15 not in any range)", result.Breakdown);
    }
}
