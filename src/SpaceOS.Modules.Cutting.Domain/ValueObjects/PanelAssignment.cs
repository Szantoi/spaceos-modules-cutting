namespace SpaceOS.Modules.Cutting.Domain.ValueObjects;

public sealed class PanelAssignment
{
    public Guid PanelStockId { get; }
    public string MaterialType { get; }
    public int PanelWidthMm { get; }
    public int PanelHeightMm { get; }
    public IReadOnlyList<PlacedPart> PlacedParts { get; }
    public int WasteAreaMm2 { get; }
    public decimal UtilizationPercent { get; }

    public PanelAssignment(
        Guid panelStockId,
        string materialType,
        int panelWidthMm,
        int panelHeightMm,
        IReadOnlyList<PlacedPart> placedParts)
    {
        PanelStockId = panelStockId;
        MaterialType = materialType;
        PanelWidthMm = panelWidthMm;
        PanelHeightMm = panelHeightMm;
        PlacedParts = placedParts;

        long totalPartArea = placedParts.Sum(p => (long)p.WidthMm * p.HeightMm);
        long panelArea = (long)panelWidthMm * panelHeightMm;
        WasteAreaMm2 = (int)(panelArea - totalPartArea);
        UtilizationPercent = panelArea > 0
            ? Math.Round((decimal)totalPartArea / panelArea * 100, 2)
            : 0m;
    }
}
