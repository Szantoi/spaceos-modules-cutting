namespace SpaceOS.Modules.Cutting.Domain.Entities;

public class CuttingBatch
{
    public Guid Id { get; private set; }
    public Guid DailyCuttingPlanId { get; private set; }
    public string MaterialType { get; private set; } = string.Empty;
    public decimal ThicknessMm { get; private set; }
    private readonly List<Guid> _sheetIds = new();
    public IReadOnlyList<Guid> SheetIds => _sheetIds.AsReadOnly();

    private CuttingBatch() { }

    public static CuttingBatch Create(Guid planId, string materialType, decimal thicknessMm, IEnumerable<Guid> sheetIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(materialType);
        if (thicknessMm <= 0) throw new ArgumentException("Thickness must be positive.", nameof(thicknessMm));

        var batch = new CuttingBatch
        {
            Id = Guid.NewGuid(),
            DailyCuttingPlanId = planId,
            MaterialType = materialType,
            ThicknessMm = thicknessMm
        };
        batch._sheetIds.AddRange(sheetIds);
        return batch;
    }
}
