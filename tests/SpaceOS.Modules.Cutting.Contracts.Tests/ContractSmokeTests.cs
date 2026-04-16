using FluentAssertions;
using Xunit;
using SpaceOS.Modules.Cutting.Contracts.Dtos;
using SpaceOS.Modules.Cutting.Contracts.Providers;
using SpaceOS.Modules.Inventory.Contracts.Dtos;
using SpaceOS.Modules.Inventory.Contracts.Providers;
using SpaceOS.Modules.Procurement.Contracts.Dtos;
using SpaceOS.Modules.Procurement.Contracts.Providers;

namespace SpaceOS.Modules.Cutting.Contracts.Tests;

public class ContractSmokeTests
{
    [Fact]
    public void IInventoryProvider_InterfaceExists()
    {
        var type = typeof(IInventoryProvider);
        type.IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IInventoryProvider_HasGetStockMethod()
    {
        var method = typeof(IInventoryProvider).GetMethod("GetStockAsync");
        method.Should().NotBeNull();
    }

    [Fact]
    public void ICuttingProvider_InterfaceExists()
    {
        var type = typeof(ICuttingProvider);
        type.IsInterface.Should().BeTrue();
    }

    [Fact]
    public void ICuttingProvider_HasSubmitCuttingSheetMethod()
    {
        var method = typeof(ICuttingProvider).GetMethod("SubmitCuttingSheetAsync");
        method.Should().NotBeNull();
    }

    [Fact]
    public void IProcurementProvider_InterfaceExists()
    {
        var type = typeof(IProcurementProvider);
        type.IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IProcurementProvider_HasCreatePurchaseOrderMethod()
    {
        var method = typeof(IProcurementProvider).GetMethod("CreatePurchaseOrderAsync");
        method.Should().NotBeNull();
    }

    [Fact]
    public void CuttingSheetDto_HasRequiredProperties()
    {
        var props = typeof(CuttingSheetDto).GetProperties().Select(p => p.Name).ToList();
        props.Should().Contain("Id");
        props.Should().Contain("TenantId");
        props.Should().Contain("Lines");
        props.Should().Contain("MaterialType");
    }

    [Fact]
    public void PurchaseOrderDto_HasRequiredProperties()
    {
        var props = typeof(PurchaseOrderDto).GetProperties().Select(p => p.Name).ToList();
        props.Should().Contain("Id");
        props.Should().Contain("TenantId");
        props.Should().Contain("SupplierId");
        props.Should().Contain("Status");
    }

    [Fact]
    public void OffcutDto_HasRequiredProperties()
    {
        var props = typeof(OffcutDto).GetProperties().Select(p => p.Name).ToList();
        props.Should().Contain("Id");
        props.Should().Contain("WidthMm");
        props.Should().Contain("HeightMm");
        props.Should().Contain("MaterialType");
    }

    [Fact]
    public void PanelStockDto_HasDimensionProperties()
    {
        var props = typeof(PanelStockDto).GetProperties().Select(p => p.Name).ToList();
        props.Should().Contain("WidthMm");
        props.Should().Contain("HeightMm");
        props.Should().Contain("FullPanelCount");
        props.Should().Contain("MaterialType");
    }
}
