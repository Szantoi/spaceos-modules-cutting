using SpaceOS.Modules.Cutting.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Domain.ValueObjects;

/// <summary>
/// Represents a single line item in a quote request.
/// </summary>
/// <param name="Material">Material type.</param>
/// <param name="WidthMm">Panel width in millimeters.</param>
/// <param name="HeightMm">Panel height in millimeters.</param>
/// <param name="Quantity">Number of identical panels.</param>
/// <param name="Edging">Edge banding type.</param>
/// <param name="Notes">Optional notes for this item.</param>
public record QuoteLineItem(
    MaterialType Material,
    int WidthMm,
    int HeightMm,
    int Quantity,
    EdgingType Edging,
    string? Notes)
{
    /// <summary>
    /// Validates line item data.
    /// </summary>
    public void Validate()
    {
        if (WidthMm <= 0 || WidthMm > 5000)
            throw new ArgumentException("Width must be between 1 and 5000 mm.", nameof(WidthMm));

        if (HeightMm <= 0 || HeightMm > 3000)
            throw new ArgumentException("Height must be between 1 and 3000 mm.", nameof(HeightMm));

        if (Quantity <= 0 || Quantity > 1000)
            throw new ArgumentException("Quantity must be between 1 and 1000.", nameof(Quantity));
    }
}
