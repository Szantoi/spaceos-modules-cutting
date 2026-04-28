namespace SpaceOS.Modules.Cutting.Execution.Domain.Entities;

/// <summary>Owned entity recording an offcut produced during execution.</summary>
public sealed class OffcutReport
{
    public Guid OffcutId { get; private set; }
    public Guid MaterialId { get; private set; }
    public decimal WidthMm { get; private set; }
    public decimal HeightMm { get; private set; }
    public decimal AreaMm2 { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private OffcutReport() { }

    internal static OffcutReport Create(Guid offcutId, Guid materialId, decimal widthMm, decimal heightMm, DateTime occurredAt)
        => new()
        {
            OffcutId = offcutId,
            MaterialId = materialId,
            WidthMm = widthMm,
            HeightMm = heightMm,
            AreaMm2 = widthMm * heightMm,
            OccurredAt = occurredAt
        };
}
