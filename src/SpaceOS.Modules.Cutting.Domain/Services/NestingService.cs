using SpaceOS.Modules.Cutting.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Domain.Services;

/// <summary>
/// L1 Nesting: Greedy First Fit Decreasing Height (FFDH) strip packing.
/// Offcuts have priority over full panels.
/// </summary>
[Obsolete("Use INestingStrategy via SpaceOS.Nesting.Algorithms NuGet. Will be removed in v1.4.0.")]
public class NestingService
{
    public IReadOnlyList<PanelAssignment> ComputeNesting(
        IReadOnlyList<CuttingLineRequest> parts,
        IReadOnlyList<AvailablePanel> panels,
        int sawBladeGapMm = 4)
    {
        if (!parts.Any()) return Array.Empty<PanelAssignment>();
        if (!panels.Any()) return Array.Empty<PanelAssignment>();

        // Sort parts by height descending (FFDH)
        var sortedParts = parts.OrderByDescending(p => p.HeightMm).ToList();

        // Offcuts first, then full panels sorted by area ascending (smallest usable first)
        var panelPool = panels
            .OrderBy(p => p.IsOffcut ? 0 : 1)
            .ThenBy(p => p.WidthMm * p.HeightMm)
            .ToList();

        var openPanels = new List<PanelState>();
        int nextPanelIndex = 0;

        foreach (var part in sortedParts)
        {
            bool placed = false;

            // Try existing open panels
            foreach (var panel in openPanels)
            {
                if (TryPlace(part, panel, sawBladeGapMm, rotate: false) ||
                    (part.CanRotate && TryPlace(part, panel, sawBladeGapMm, rotate: true)))
                {
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                // Open next panel from pool — skip panels too small even when rotated
                while (nextPanelIndex < panelPool.Count)
                {
                    var candidate = panelPool[nextPanelIndex++];
                    var fits = part.WidthMm <= candidate.WidthMm && part.HeightMm <= candidate.HeightMm;
                    var fitsRotated = part.CanRotate &&
                                     part.HeightMm <= candidate.WidthMm && part.WidthMm <= candidate.HeightMm;
                    if (!fits && !fitsRotated) continue;

                    var newPanel = new PanelState(candidate);
                    openPanels.Add(newPanel);

                    if (TryPlace(part, newPanel, sawBladeGapMm, rotate: false) ||
                        (part.CanRotate && TryPlace(part, newPanel, sawBladeGapMm, rotate: true)))
                    {
                        placed = true;
                    }
                    break;
                }
            }

            // If still not placed, the part cannot fit — skip it (graceful degradation)
        }

        return openPanels.Select(p => p.ToAssignment()).ToList();
    }

    private static bool TryPlace(CuttingLineRequest part, PanelState panel, int gap, bool rotate)
    {
        var w = rotate ? part.HeightMm : part.WidthMm;
        var h = rotate ? part.WidthMm : part.HeightMm;

        // Try existing shelves (first fit)
        foreach (var shelf in panel.Shelves)
        {
            if (shelf.CurrentX + w <= panel.Panel.WidthMm)
            {
                var x = shelf.CurrentX;
                var y = shelf.Y;
                shelf.Place(w, gap);
                panel.PlacedParts.Add(new PlacedPart(part.PartName, (int)x, y, (int)w, (int)h, rotate));
                return true;
            }
        }

        // Try new shelf
        var nextY = panel.Shelves.Count == 0
            ? 0
            : panel.Shelves.Sum(s => s.Height + gap);

        if (nextY + h <= panel.Panel.HeightMm && w <= panel.Panel.WidthMm)
        {
            var shelf = new Shelf(nextY, (int)h);
            shelf.Place(w, gap);
            panel.Shelves.Add(shelf);
            panel.PlacedParts.Add(new PlacedPart(part.PartName, 0, nextY, (int)w, (int)h, rotate));
            return true;
        }

        return false;
    }

    // Internal helpers
    private sealed class PanelState
    {
        public AvailablePanel Panel { get; }
        public List<Shelf> Shelves { get; } = new();
        public List<PlacedPart> PlacedParts { get; } = new();

        public PanelState(AvailablePanel panel) => Panel = panel;

        public PanelAssignment ToAssignment() => new(
            Panel.PanelStockId,
            Panel.MaterialType,
            (int)Panel.WidthMm,
            (int)Panel.HeightMm,
            PlacedParts);
    }

    private sealed class Shelf
    {
        public int Y { get; }
        public int Height { get; }
        public decimal CurrentX { get; private set; }

        public Shelf(int y, int height)
        {
            Y = y;
            Height = height;
            CurrentX = 0;
        }

        public void Place(decimal partWidth, int gap)
        {
            CurrentX += partWidth + gap;
        }
    }
}
