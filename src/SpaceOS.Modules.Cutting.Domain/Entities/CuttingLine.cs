namespace SpaceOS.Modules.Cutting.Domain.Entities;

public class CuttingLine
{
    public Guid Id { get; private set; }
    public Guid CuttingSheetId { get; private set; }
    public string PartName { get; private set; } = string.Empty;
    public string MaterialType { get; private set; } = string.Empty;
    public decimal WidthMm { get; private set; }
    public decimal HeightMm { get; private set; }
    public decimal ThicknessMm { get; private set; }
    public int Quantity { get; private set; }
    public string? Notes { get; private set; }

    private CuttingLine() { }

    public static CuttingLine Create(Guid cuttingSheetId, string partName, string materialType,
        decimal widthMm, decimal heightMm, decimal thicknessMm, int quantity, string? notes = null)
    {
        // Note: cuttingSheetId can be Guid.Empty for template lines (recreated by CuttingSheet.Create)
        ArgumentException.ThrowIfNullOrWhiteSpace(partName);
        if (widthMm <= 0) throw new ArgumentException("Width must be positive.", nameof(widthMm));
        if (heightMm <= 0) throw new ArgumentException("Height must be positive.", nameof(heightMm));
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        return new CuttingLine
        {
            Id = Guid.NewGuid(),
            CuttingSheetId = cuttingSheetId,
            PartName = partName,
            MaterialType = materialType,
            WidthMm = widthMm,
            HeightMm = heightMm,
            ThicknessMm = thicknessMm,
            Quantity = quantity,
            Notes = notes
        };
    }
}
