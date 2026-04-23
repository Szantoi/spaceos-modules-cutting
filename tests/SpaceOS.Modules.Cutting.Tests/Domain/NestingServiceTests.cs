using FluentAssertions;
#pragma warning disable CS0618 // NestingService is obsolete — these tests verify legacy behaviour
using SpaceOS.Modules.Cutting.Domain.Services;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Domain;

public class NestingServiceTests
{
    private readonly NestingService _sut = new();
    private static readonly Guid PanelId = Guid.NewGuid();

    private static AvailablePanel FullPanel(decimal w = 2800, decimal h = 2070) =>
        new(PanelId, "MDF 18mm", w, h, IsOffcut: false);

    private static AvailablePanel Offcut(decimal w, decimal h) =>
        new(Guid.NewGuid(), "MDF 18mm", w, h, IsOffcut: true);

    // ── 1 part, 1 panel → 1 assignment, placed at (0,0) ──────────────────────

    [Fact]
    public void OnePart_OnePanelFits_PlacedAtOrigin()
    {
        var parts = new[] { new CuttingLineRequest("Door", 600, 2000) };
        var panels = new[] { FullPanel() };

        var result = _sut.ComputeNesting(parts, panels);

        result.Should().HaveCount(1);
        var placed = result[0].PlacedParts.Single();
        placed.X.Should().Be(0);
        placed.Y.Should().Be(0);
        placed.PartName.Should().Be("Door");
    }

    // ── 2 parts side by side on 1 panel ───────────────────────────────────────

    [Fact]
    public void TwoParts_FitSideBySide_OnePanelTwoPlacements()
    {
        var parts = new[]
        {
            new CuttingLineRequest("Left",  600, 800),
            new CuttingLineRequest("Right", 600, 800)
        };
        var panels = new[] { FullPanel() };

        var result = _sut.ComputeNesting(parts, panels);

        result.Should().HaveCount(1);
        result[0].PlacedParts.Should().HaveCount(2);
    }

    // ── 2 parts don't fit on 1 panel → 2 panels ───────────────────────────────

    [Fact]
    public void TwoParts_NoFitOnOnePanel_TwoPanelsUsed()
    {
        // Panel is exactly 1000 wide; each part is 600 wide — 600+4+600=1204 > 1000
        var parts = new[]
        {
            new CuttingLineRequest("A", 600, 500),
            new CuttingLineRequest("B", 600, 500)
        };
        var panels = new[]
        {
            new AvailablePanel(Guid.NewGuid(), "MDF 18mm", 1000, 600, false),
            new AvailablePanel(Guid.NewGuid(), "MDF 18mm", 1000, 600, false)
        };

        var result = _sut.ComputeNesting(parts, panels);

        result.Should().HaveCount(2);
        result.Sum(a => a.PlacedParts.Count).Should().Be(2);
    }

    // ── Saw blade gap respected ────────────────────────────────────────────────

    [Fact]
    public void SawBladeGap_IsIncludedInXPosition()
    {
        var parts = new[]
        {
            new CuttingLineRequest("A", 300, 500),
            new CuttingLineRequest("B", 300, 500)
        };
        var panels = new[] { FullPanel() };

        var result = _sut.ComputeNesting(parts, panels, sawBladeGapMm: 4);

        var placements = result[0].PlacedParts.OrderBy(p => p.X).ToList();
        placements[0].X.Should().Be(0);
        placements[1].X.Should().Be(304); // 300 + 4mm gap
    }

    // ── Parts sorted descending by height ─────────────────────────────────────

    [Fact]
    public void TallPartFirst_FitsInFirstShelf_ShorterPartInSameShelf()
    {
        // Tall part (H=1000) opens a shelf; shorter part (H=500) fits in same shelf
        var parts = new[]
        {
            new CuttingLineRequest("Short", 400, 500),
            new CuttingLineRequest("Tall",  400, 1000)
        };
        var panels = new[] { FullPanel() };

        var result = _sut.ComputeNesting(parts, panels);

        // Both on same panel, same shelf (Y=0)
        result.Should().HaveCount(1);
        result[0].PlacedParts.Should().HaveCount(2);
        result[0].PlacedParts.All(p => p.Y == 0).Should().BeTrue();
    }

    // ── Rotation: part fits only rotated ──────────────────────────────────────

    [Fact]
    public void Part_FitsOnlyRotated_IsPlacedRotated()
    {
        // Panel: 500 wide, 2000 tall
        // Part: 400 wide, 600 tall — fits as-is (400 ≤ 500, 600 ≤ 2000)
        // But let's make it only fit rotated: part 600 wide, 300 tall, panel 400 wide
        var panels = new[] { new AvailablePanel(Guid.NewGuid(), "MDF 18mm", 400, 2000, false) };
        var parts = new[] { new CuttingLineRequest("BigPart", 600, 300, CanRotate: true) };

        var result = _sut.ComputeNesting(parts, panels);

        result.Should().HaveCount(1);
        var placed = result[0].PlacedParts.Single();
        placed.IsRotated.Should().BeTrue();
        placed.WidthMm.Should().Be(300);
        placed.HeightMm.Should().Be(600);
    }

    // ── Utilization % calculated correctly ────────────────────────────────────

    [Fact]
    public void UtilizationPercent_IsCorrect()
    {
        // Panel 1000x1000=1_000_000 mm², part 500x500=250_000 mm² → 25%
        var panels = new[] { new AvailablePanel(Guid.NewGuid(), "MDF 18mm", 1000, 1000, false) };
        var parts = new[] { new CuttingLineRequest("Sq", 500, 500) };

        var result = _sut.ComputeNesting(parts, panels);

        result[0].UtilizationPercent.Should().Be(25m);
    }

    // ── Waste area calculated correctly ───────────────────────────────────────

    [Fact]
    public void WasteAreaMm2_IsCorrect()
    {
        // Panel 1000x1000=1_000_000, part 500x500=250_000 → waste 750_000
        var panels = new[] { new AvailablePanel(Guid.NewGuid(), "MDF 18mm", 1000, 1000, false) };
        var parts = new[] { new CuttingLineRequest("Sq", 500, 500) };

        var result = _sut.ComputeNesting(parts, panels);

        result[0].WasteAreaMm2.Should().Be(750_000);
    }

    // ── Offcut preferred over full panel ──────────────────────────────────────

    [Fact]
    public void Offcut_UsedBeforeFullPanel()
    {
        var offcutId = Guid.NewGuid();
        var fullPanelId = Guid.NewGuid();

        // Offcut exactly fits the part
        var panels = new[]
        {
            new AvailablePanel(fullPanelId, "MDF 18mm", 2800, 2070, IsOffcut: false),
            new AvailablePanel(offcutId,    "MDF 18mm", 700,  600,  IsOffcut: true)
        };
        var parts = new[] { new CuttingLineRequest("Part", 600, 500) };

        var result = _sut.ComputeNesting(parts, panels);

        result.Should().HaveCount(1);
        result[0].PanelStockId.Should().Be(offcutId, "offcut should be used first");
    }

    // ── Empty parts → empty result ────────────────────────────────────────────

    [Fact]
    public void NoParts_ReturnsEmpty()
    {
        var result = _sut.ComputeNesting(Array.Empty<CuttingLineRequest>(), new[] { FullPanel() });
        result.Should().BeEmpty();
    }

    // ── No panels → empty result (graceful) ───────────────────────────────────

    [Fact]
    public void NoPanels_ReturnsEmpty()
    {
        var parts = new[] { new CuttingLineRequest("Part", 600, 500) };
        var result = _sut.ComputeNesting(parts, Array.Empty<AvailablePanel>());
        result.Should().BeEmpty();
    }

    // ── Multiple parts fill multiple shelves ──────────────────────────────────

    [Fact]
    public void MultipleShelvesOnOnePanel()
    {
        // Panel 1000 wide, 2000 tall. Part H=900 → fills first shelf (1 part).
        // Second part H=900 → second shelf (Y=900+4=904)
        var panels = new[] { new AvailablePanel(Guid.NewGuid(), "MDF 18mm", 1000, 2000, false) };
        var parts = new[]
        {
            new CuttingLineRequest("Top",    800, 900),
            new CuttingLineRequest("Bottom", 800, 900)
        };

        var result = _sut.ComputeNesting(parts, panels);

        result.Should().HaveCount(1);
        var placements = result[0].PlacedParts.OrderBy(p => p.Y).ToList();
        placements[0].Y.Should().Be(0);
        placements[1].Y.Should().Be(904); // 900 + 4mm gap
    }

    // ── Part that fits neither way is gracefully skipped ─────────────────────

    [Fact]
    public void PartTooLargeForAllPanels_IsSkipped()
    {
        var panels = new[] { new AvailablePanel(Guid.NewGuid(), "MDF 18mm", 500, 500, false) };
        var parts = new[]
        {
            new CuttingLineRequest("Small", 200, 200),
            new CuttingLineRequest("Huge",  600, 600, CanRotate: false) // won't fit
        };

        var act = () => _sut.ComputeNesting(parts, panels);
        act.Should().NotThrow("oversized parts are skipped gracefully");
    }

    // ── PartName preserved in placement ───────────────────────────────────────

    [Fact]
    public void PlacedPart_PreservesPartName()
    {
        var parts = new[] { new CuttingLineRequest("SidePanel", 400, 600) };
        var panels = new[] { FullPanel() };

        var result = _sut.ComputeNesting(parts, panels);

        result[0].PlacedParts.Single().PartName.Should().Be("SidePanel");
    }

    // ── MaterialType preserved in assignment ──────────────────────────────────

    [Fact]
    public void PanelAssignment_PreservesMaterialType()
    {
        var panels = new[] { new AvailablePanel(Guid.NewGuid(), "HDF 3mm", 2800, 2070, false) };
        var parts = new[] { new CuttingLineRequest("Back", 600, 400) };

        var result = _sut.ComputeNesting(parts, panels);

        result[0].MaterialType.Should().Be("HDF 3mm");
    }
}
