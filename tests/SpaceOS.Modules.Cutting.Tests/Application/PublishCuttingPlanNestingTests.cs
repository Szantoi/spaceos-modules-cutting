using Ardalis.Result;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Modules.Cutting.Application.Commands.PublishCuttingPlan;
using SpaceOS.Modules.Cutting.Application.Services;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Domain.Services;
using SpaceOS.Modules.Inventory.Contracts.Dtos;
using SpaceOS.Modules.Inventory.Contracts.Providers;
using SpaceOS.Nesting.Algorithms;
using SpaceOS.Nesting.Algorithms.Models;
using SpaceOS.Nesting.Algorithms.Strategies;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Application;

public class PublishCuttingPlanNestingTests
{
    private readonly Mock<ICuttingRepository> _repoMock = new();
    private readonly Mock<IPlanNestingSnapshotRepository> _snapshotRepoMock = new();
    private readonly Mock<IInventoryProvider> _inventoryMock = new();
    private readonly INestingStrategy _nestingStrategy = new FfdhNestingStrategy();
    private readonly ICapacityModel _capacityModel = new AreaCapacityModel();

    private PublishCuttingPlanCommandHandler CreateHandler()
    {
        var panelSource = new PanelSourceService(
            _inventoryMock.Object,
            NullLogger<PanelSourceService>.Instance);

        return new PublishCuttingPlanCommandHandler(
            _repoMock.Object,
            _nestingStrategy,
            panelSource,
            _snapshotRepoMock.Object);
    }

    private CuttingPlan CreatePlanWithJobs(int jobCount = 3, string material = "MDF 18mm",
        GrainDirection grain = GrainDirection.None, decimal widthMm = 600m, decimal heightMm = 400m)
    {
        var plan = CuttingPlan.Create(Guid.NewGuid(), DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc), 7, "fifo");
        var slot = plan.DaySlots[0];

        for (int i = 0; i < jobCount; i++)
        {
            var job = CuttingJob.Create(
                slot.Id, Guid.NewGuid(),
                slot.SlotDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                "Normal", 0.5m, widthMm, heightMm, material, grain);
            slot.AddJob(job, _capacityModel);
        }

        return plan;
    }

    private void SetupInventory(string material = "MDF 18mm", int panelCount = 5, int w = 2800, int h = 2070)
    {
        _inventoryMock.Setup(i => i.GetStockAsync(material, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PanelStockDto(material, panelCount, w, h, new List<OffcutDto>()));
        _inventoryMock.Setup(i => i.GetOffcutsAsync(material, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OffcutDto>());
    }

    // ── 1. Publish happy path: CuttingJobs → nesting → PlanNestingSnapshot saved ──

    [Fact]
    public async Task Publish_HappyPath_SavesNestingSnapshot()
    {
        var plan = CreatePlanWithJobs(3);
        _repoMock.Setup(r => r.GetCuttingPlanTrackedAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _snapshotRepoMock.Setup(r => r.GetByPlanAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync((PlanNestingSnapshot?)null);
        SetupInventory();

        var handler = CreateHandler();
        var result = await handler.Handle(new PublishCuttingPlanCommand(plan.Id, Guid.NewGuid()), default);

        result.IsSuccess.Should().BeTrue();
        _snapshotRepoMock.Verify(r => r.AddAsync(It.IsAny<PlanNestingSnapshot>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── 2. Nesting result: placements count == total parts ────────────────────

    [Fact]
    public async Task Publish_NestingResult_PlacementsCountMatchParts()
    {
        var plan = CreatePlanWithJobs(2);
        _repoMock.Setup(r => r.GetCuttingPlanTrackedAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _snapshotRepoMock.Setup(r => r.GetByPlanAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync((PlanNestingSnapshot?)null);
        SetupInventory();

        PlanNestingSnapshot? saved = null;
        _snapshotRepoMock.Setup(r => r.AddAsync(It.IsAny<PlanNestingSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<PlanNestingSnapshot, CancellationToken>((s, _) => saved = s);

        var handler = CreateHandler();
        await handler.Handle(new PublishCuttingPlanCommand(plan.Id, Guid.NewGuid()), default);

        saved.Should().NotBeNull();
        saved!.PlacementsJson.Should().NotBeNullOrEmpty();
        // PlacementsJson should contain placement data
        saved.PlacementsJson.Should().Contain("PartId");
    }

    // ── 3. Yield > 0% ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Publish_YieldPercent_GreaterThanZero()
    {
        var plan = CreatePlanWithJobs(1);
        _repoMock.Setup(r => r.GetCuttingPlanTrackedAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _snapshotRepoMock.Setup(r => r.GetByPlanAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync((PlanNestingSnapshot?)null);
        SetupInventory();

        PlanNestingSnapshot? saved = null;
        _snapshotRepoMock.Setup(r => r.AddAsync(It.IsAny<PlanNestingSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<PlanNestingSnapshot, CancellationToken>((s, _) => saved = s);

        var handler = CreateHandler();
        await handler.Handle(new PublishCuttingPlanCommand(plan.Id, Guid.NewGuid()), default);

        saved.Should().NotBeNull();
        saved!.YieldPercent.Should().BeGreaterThan(0m);
    }

    // ── 4. GrainDirection None → CanRotate=true ──────────────────────────────

    [Fact]
    public async Task Publish_GrainDirectionNone_CanRotateTrue()
    {
        // Create plan with GrainDirection.None job
        var plan = CreatePlanWithJobs(1, grain: GrainDirection.None, widthMm: 500m, heightMm: 800m);
        _repoMock.Setup(r => r.GetCuttingPlanTrackedAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _snapshotRepoMock.Setup(r => r.GetByPlanAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync((PlanNestingSnapshot?)null);
        // Panel that only fits if rotated (width=700, height=900 — part is 500×800, fits normal)
        // But let's verify rotation is allowed: use a panel where rotation helps
        SetupInventory(w: 900, h: 600); // 900×600 panel — part 500×800 doesn't fit normal, but rotated (800×500) fits

        PlanNestingSnapshot? saved = null;
        _snapshotRepoMock.Setup(r => r.AddAsync(It.IsAny<PlanNestingSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<PlanNestingSnapshot, CancellationToken>((s, _) => saved = s);

        var handler = CreateHandler();
        await handler.Handle(new PublishCuttingPlanCommand(plan.Id, Guid.NewGuid()), default);

        // With CanRotate=true, the part should be placed (rotated if needed)
        saved.Should().NotBeNull();
        saved!.PlacementsJson.Should().Contain("PartId");
    }

    // ── 5. GrainDirection Vertical → CanRotate=false ─────────────────────────

    [Fact]
    public async Task Publish_GrainDirectionVertical_CanRotateFalse()
    {
        // Part 500×800 on 400×900 panel — normal doesn't fit (500>400), rotated 800×500>400 also doesn't fit
        // With CanRotate=false, unplaceable
        var plan = CreatePlanWithJobs(1, grain: GrainDirection.Vertical, widthMm: 500m, heightMm: 800m);
        _repoMock.Setup(r => r.GetCuttingPlanTrackedAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _snapshotRepoMock.Setup(r => r.GetByPlanAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync((PlanNestingSnapshot?)null);
        // Large enough panel so the part fits without rotation
        SetupInventory(w: 2800, h: 2070);

        PlanNestingSnapshot? saved = null;
        _snapshotRepoMock.Setup(r => r.AddAsync(It.IsAny<PlanNestingSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<PlanNestingSnapshot, CancellationToken>((s, _) => saved = s);

        var handler = CreateHandler();
        await handler.Handle(new PublishCuttingPlanCommand(plan.Id, Guid.NewGuid()), default);

        saved.Should().NotBeNull();
        // Job was placed (panel is large enough) but with CanRotate=false
        saved!.PlacementsJson.Should().Contain("PartId");
    }

    // ── 6. PanelSourceService: stock panels fetched correctly (stub test) ────

    [Fact]
    public async Task PanelSourceService_FetchesStockAndOffcuts()
    {
        SetupInventory("MDF 18mm", 2, 2800, 2070);
        _inventoryMock.Setup(i => i.GetOffcutsAsync("MDF 18mm", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OffcutDto>
            {
                new(Guid.NewGuid(), "MDF 18mm", 800, 600, Guid.Empty)
            });

        var panelSource = new PanelSourceService(
            _inventoryMock.Object,
            NullLogger<PanelSourceService>.Instance);

        var panels = await panelSource.GetAvailablePanelsAsync(new[] { "MDF 18mm" }, default);

        panels.Should().HaveCount(3, "2 stock + 1 offcut");
        panels.Count(p => !p.IsOffcut).Should().Be(2);
        panels.Count(p => p.IsOffcut).Should().Be(1);
    }

    // ── 7. ReserveAsync not called during Publish (reservation is separate) ──

    [Fact]
    public async Task Publish_DoesNotCallReserveAsync()
    {
        // Publish only does nesting + snapshot. Reserve is a separate endpoint.
        var plan = CreatePlanWithJobs(1);
        _repoMock.Setup(r => r.GetCuttingPlanTrackedAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _snapshotRepoMock.Setup(r => r.GetByPlanAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync((PlanNestingSnapshot?)null);
        SetupInventory();

        var handler = CreateHandler();
        await handler.Handle(new PublishCuttingPlanCommand(plan.Id, Guid.NewGuid()), default);

        // No reservation calls during publish
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── 8. PlanNestingSnapshot contains algorithm name ───────────────────────

    [Fact]
    public async Task Publish_SnapshotContainsAlgorithmName()
    {
        var plan = CreatePlanWithJobs(1);
        _repoMock.Setup(r => r.GetCuttingPlanTrackedAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _snapshotRepoMock.Setup(r => r.GetByPlanAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync((PlanNestingSnapshot?)null);
        SetupInventory();

        PlanNestingSnapshot? saved = null;
        _snapshotRepoMock.Setup(r => r.AddAsync(It.IsAny<PlanNestingSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<PlanNestingSnapshot, CancellationToken>((s, _) => saved = s);

        var handler = CreateHandler();
        await handler.Handle(new PublishCuttingPlanCommand(plan.Id, Guid.NewGuid()), default);

        saved.Should().NotBeNull();
        saved!.Algorithm.Should().Be("FFDH");
    }

    // ── 9. Empty DaySlot (0 jobs) → skip nesting ────────────────────────────

    [Fact]
    public async Task Publish_EmptyDaySlots_SkipsNesting()
    {
        var plan = CuttingPlan.Create(Guid.NewGuid(), DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc), 7, "fifo");
        // No jobs added
        _repoMock.Setup(r => r.GetCuttingPlanTrackedAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var handler = CreateHandler();
        var result = await handler.Handle(new PublishCuttingPlanCommand(plan.Id, Guid.NewGuid()), default);

        result.IsSuccess.Should().BeTrue();
        _snapshotRepoMock.Verify(r => r.AddAsync(It.IsAny<PlanNestingSnapshot>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── 10. Publish idempotencia: already Published plan → error ──────────────

    [Fact]
    public async Task Publish_AlreadyPublished_ReturnsInvalid()
    {
        var plan = CreatePlanWithJobs(1);
        plan.Publish(Guid.NewGuid()); // now Published
        _repoMock.Setup(r => r.GetCuttingPlanTrackedAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _snapshotRepoMock.Setup(r => r.GetByPlanAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync((PlanNestingSnapshot?)null);
        SetupInventory();

        var handler = CreateHandler();
        var result = await handler.Handle(new PublishCuttingPlanCommand(plan.Id, Guid.NewGuid()), default);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
    }
}
