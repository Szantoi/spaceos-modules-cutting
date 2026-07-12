namespace SpaceOS.Modules.Cutting.Domain.Entities;

/// <summary>
/// Entity representing a complexity modifier for pricing calculations.
/// Q3 Track B — Pricing Integration.
/// </summary>
public class ComplexityModifier
{
    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the price list identifier this modifier belongs to.</summary>
    public Guid PriceListId { get; private set; }

    /// <summary>
    /// Gets the modifier type (e.g., "CutCount", "ShapeComplexity", "EdgeBanding").
    /// </summary>
    public string ModifierType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the multiplier value applied to pricing calculations.
    /// Example: 0.10 for "CutCount" means each cut adds 10% complexity.
    /// </summary>
    public decimal MultiplierValue { get; private set; }

    /// <summary>Parameterless constructor for EF Core.</summary>
    private ComplexityModifier() { }

    /// <summary>
    /// Creates a new complexity modifier instance.
    /// </summary>
    /// <param name="priceListId">The price list identifier.</param>
    /// <param name="modifierType">The modifier type.</param>
    /// <param name="multiplierValue">The multiplier value.</param>
    /// <returns>A new complexity modifier instance.</returns>
    internal static ComplexityModifier Create(
        Guid priceListId,
        string modifierType,
        decimal multiplierValue)
    {
        return new ComplexityModifier
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ModifierType = modifierType,
            MultiplierValue = multiplierValue
        };
    }

    /// <summary>
    /// Updates the multiplier value.
    /// </summary>
    /// <param name="newValue">The new multiplier value.</param>
    /// <exception cref="ArgumentException">Thrown when value is negative.</exception>
    public void UpdateMultiplier(decimal newValue)
    {
        if (newValue < 0)
            throw new ArgumentException("Multiplier value cannot be negative.", nameof(newValue));

        MultiplierValue = newValue;
    }
}
