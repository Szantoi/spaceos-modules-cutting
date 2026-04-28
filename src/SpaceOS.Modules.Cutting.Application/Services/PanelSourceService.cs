using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Inventory.Contracts.Providers;
using SpaceOS.Nesting.Algorithms.Models;

namespace SpaceOS.Modules.Cutting.Application.Services;

/// <summary>
/// Collects available panels (stock + usable offcuts) from the Inventory service
/// and maps them to NestingInput.AvailablePanel instances.
/// Degrades gracefully if Inventory is unavailable.
/// </summary>
public sealed class PanelSourceService
{
    private readonly IInventoryProvider _inventoryProvider;
    private readonly ILogger<PanelSourceService> _logger;

    public PanelSourceService(IInventoryProvider inventoryProvider, ILogger<PanelSourceService> logger)
    {
        _inventoryProvider = inventoryProvider;
        _logger = logger;
    }

    /// <summary>
    /// Fetches stock panels and usable offcuts for the given material codes.
    /// Returns an empty list if Inventory is unavailable.
    /// </summary>
    public async Task<IReadOnlyList<AvailablePanel>> GetAvailablePanelsAsync(
        IEnumerable<string> materialCodes,
        CancellationToken ct)
    {
        var panels = new List<AvailablePanel>();

        foreach (var material in materialCodes.Distinct())
        {
            try
            {
                var stock = await _inventoryProvider.GetStockAsync(material, ct).ConfigureAwait(false);
                if (stock.WidthMm > 0 && stock.HeightMm > 0)
                {
                    for (int i = 0; i < stock.FullPanelCount; i++)
                    {
                        panels.Add(new AvailablePanel(
                            PanelId: Guid.NewGuid().ToString(),
                            MaterialCode: material,
                            WidthMm: stock.WidthMm,
                            HeightMm: stock.HeightMm,
                            IsOffcut: false));
                    }
                }

                var offcuts = await _inventoryProvider.GetOffcutsAsync(material, ct).ConfigureAwait(false);
                foreach (var offcut in offcuts)
                {
                    panels.Add(new AvailablePanel(
                        PanelId: offcut.Id.ToString(),
                        MaterialCode: material,
                        WidthMm: offcut.WidthMm,
                        HeightMm: offcut.HeightMm,
                        IsOffcut: true));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "PanelSourceService: Inventory unavailable for material {Material}. Skipping.", material);
            }
        }

        return panels;
    }
}
