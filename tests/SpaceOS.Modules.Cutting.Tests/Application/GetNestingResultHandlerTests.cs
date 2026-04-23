using Ardalis.Result;
using FluentAssertions;
using Moq;
using SpaceOS.Modules.Cutting.Application.Queries.GetNestingResult;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Inventory.Contracts.Dtos;
using SpaceOS.Nesting.Algorithms;
using SpaceOS.Nesting.Algorithms.Strategies;
using SpaceOS.Modules.Inventory.Contracts.Providers;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Application;

public class GetNestingResultHandlerTests
{
    private readonly Mock<ICuttingRepository> _repoMock = new();
    private readonly Mock<IInventoryProvider> _inventoryMock = new();
    private readonly Mock<IPlanNestingSnapshotRepository> _snapshotRepoMock = new();
    private readonly INestingStrategy _nestingStrategy = new FfdhNestingStrategy();

    private GetNestingResultQueryHandler CreateHandler() =>
        new(_repoMock.Object, _inventoryMock.Object, _nestingStrategy, _snapshotRepoMock.Object);

    private static CuttingSheet CreateSheet()
    {
        var lines = new[]
        {
            CuttingLine.Create(Guid.NewGuid(), "Door", "MDF 18mm", 600, 2000, 18, 1)
        };
        var sheet = CuttingSheet.Create(Guid.NewGuid(), "ORD-001", lines);
        return sheet;
    }

    [Fact]
    public async Task Handle_WithInventory_ReturnsPanelAssignments()
    {
        var sheet = CreateSheet();
        _repoMock.Setup(r => r.GetCuttingSheetByIdAsync(sheet.Id, default)).ReturnsAsync(sheet);

        _inventoryMock.Setup(i => i.GetStockAsync("MDF 18mm", default))
            .ReturnsAsync(new PanelStockDto("MDF 18mm", 2, 2800, 2070, new List<OffcutDto>()));
        _inventoryMock.Setup(i => i.GetOffcutsAsync("MDF 18mm", default))
            .ReturnsAsync(new List<OffcutDto>());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetNestingResultQuery(sheet.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.PanelAssignments.Should().NotBeNull();
        result.Value.PanelAssignments!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_InventoryThrows_ReturnsGroupingWithoutAssignments()
    {
        var sheet = CreateSheet();
        _repoMock.Setup(r => r.GetCuttingSheetByIdAsync(sheet.Id, default)).ReturnsAsync(sheet);

        _inventoryMock.Setup(i => i.GetStockAsync(It.IsAny<string>(), default))
            .ThrowsAsync(new HttpRequestException("Inventory unreachable"));

        var handler = CreateHandler();
        var result = await handler.Handle(new GetNestingResultQuery(sheet.Id), default);

        result.IsSuccess.Should().BeTrue("graceful degradation");
        result.Value.PanelAssignments.Should().BeNull("no panels when inventory fails");
        result.Value.Groups.Should().NotBeEmpty("grouping still works");
    }

    [Fact]
    public async Task Handle_InventoryReturnsDifferentPanelSizes_NestingUsesCorrectDimensions()
    {
        // Sheet with a part that fits 2440×1220 but NOT 1000×500
        var lines = new[] { CuttingLine.Create(Guid.NewGuid(), "BigDoor", "MDF 18mm", 900, 1100, 18, 1) };
        var sheet = CuttingSheet.Create(Guid.NewGuid(), "ORD-002", lines);
        _repoMock.Setup(r => r.GetCuttingSheetByIdAsync(sheet.Id, default)).ReturnsAsync(sheet);

        // Small panel first (won't fit), big panel second
        _inventoryMock.SetupSequence(i => i.GetStockAsync("MDF 18mm", default))
            .ReturnsAsync(new PanelStockDto("MDF 18mm", 1, 2440, 1220, new List<OffcutDto>()));
        _inventoryMock.Setup(i => i.GetOffcutsAsync("MDF 18mm", default))
            .ReturnsAsync(new List<OffcutDto>());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetNestingResultQuery(sheet.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.PanelAssignments.Should().NotBeNull();
        // Panel dimensions from DTO (not hardcoded 2800×2070)
        result.Value.PanelAssignments!.Should().Contain(a =>
            a.PanelWidthMm == 2440 && a.PanelHeightMm == 1220);
    }

    [Fact]
    public async Task Handle_SheetNotFound_ReturnsNotFound()
    {
        _repoMock.Setup(r => r.GetCuttingSheetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((CuttingSheet?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetNestingResultQuery(Guid.NewGuid()), default);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_WithPlanId_SavesSnapshot_IfNotExists()
    {
        var sheet = CreateSheet();
        var planId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _repoMock.Setup(r => r.GetCuttingSheetByIdAsync(sheet.Id, default)).ReturnsAsync(sheet);
        _inventoryMock.Setup(i => i.GetStockAsync("MDF 18mm", default))
            .ReturnsAsync(new PanelStockDto("MDF 18mm", 2, 2800, 2070, new List<OffcutDto>()));
        _inventoryMock.Setup(i => i.GetOffcutsAsync("MDF 18mm", default))
            .ReturnsAsync(new List<OffcutDto>());

        // No existing snapshot
        _snapshotRepoMock.Setup(r => r.GetByPlanAsync(planId, default))
            .ReturnsAsync((PlanNestingSnapshot?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetNestingResultQuery(sheet.Id, planId, tenantId), default);

        result.IsSuccess.Should().BeTrue();
        _snapshotRepoMock.Verify(r => r.AddAsync(It.IsAny<PlanNestingSnapshot>(), default), Times.Once);
        _snapshotRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}
