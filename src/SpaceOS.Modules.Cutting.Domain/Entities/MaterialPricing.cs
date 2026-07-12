namespace SpaceOS.Modules.Cutting.Domain.Entities;

/// <summary>
/// Entity representing pricing for a specific material type.
/// Q3 Track B — Pricing Integration.
/// </summary>
public class MaterialPricing
{
    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the price list identifier this pricing belongs to.</summary>
    public Guid PriceListId { get; private set; }

    /// <summary>Gets the material type (e.g., "MDF", "Plywood", "Chipboard").</summary>
    public string MaterialType { get; private set; } = string.Empty;

    /// <summary>Gets the price per square meter.</summary>
    public decimal PricePerSquareMeter { get; private set; }

    /// <summary>Gets the currency code (e.g., "HUF", "EUR").</summary>
    public string Currency { get; private set; } = "HUF";

    /// <summary>Parameterless constructor for EF Core.</summary>
    private MaterialPricing() { }

    /// <summary>
    /// Creates a new material pricing instance.
    /// </summary>
    /// <param name="priceListId">The price list identifier.</param>
    /// <param name="materialType">The material type.</param>
    /// <param name="pricePerM2">Price per square meter.</param>
    /// <param name="currency">Currency code.</param>
    /// <returns>A new material pricing instance.</returns>
    internal static MaterialPricing Create(
        Guid priceListId,
        string materialType,
        decimal pricePerM2,
        string currency = "HUF")
    {
        return new MaterialPricing
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            MaterialType = materialType,
            PricePerSquareMeter = pricePerM2,
            Currency = currency
        };
    }

    /// <summary>
    /// Updates the price for this material.
    /// </summary>
    /// <param name="newPrice">The new price per square meter.</param>
    /// <exception cref="ArgumentException">Thrown when price is invalid.</exception>
    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new ArgumentException("Price must be greater than zero.", nameof(newPrice));

        PricePerSquareMeter = newPrice;
    }
}
