using Ardalis.Result;

namespace SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;

/// <summary>Carries the data for an offcut that was produced during execution.</summary>
public sealed record OffcutEvent
{
    public Guid MaterialId { get; }
    public decimal WidthMm { get; }
    public decimal HeightMm { get; }

    private OffcutEvent(Guid materialId, decimal widthMm, decimal heightMm)
    {
        MaterialId = materialId;
        WidthMm = widthMm;
        HeightMm = heightMm;
    }

    /// <summary>Creates an OffcutEvent, validating that dimensions are positive and materialId non-empty.</summary>
    public static Result<OffcutEvent> Create(Guid materialId, decimal widthMm, decimal heightMm)
    {
        if (materialId == Guid.Empty)
            return Result<OffcutEvent>.Invalid(new ValidationError("MaterialId must not be empty."));
        if (widthMm <= 0)
            return Result<OffcutEvent>.Invalid(new ValidationError("Width must be positive."));
        if (heightMm <= 0)
            return Result<OffcutEvent>.Invalid(new ValidationError("Height must be positive."));
        return Result<OffcutEvent>.Success(new OffcutEvent(materialId, widthMm, heightMm));
    }

    public decimal AreaMm2 => WidthMm * HeightMm;
}
