using SpaceOS.Modules.Cutting.Domain.Entities;

namespace SpaceOS.Modules.Cutting.Domain.Aggregates;

/// <summary>
/// Aggregate root representing a pricing configuration for a tenant.
/// Q3 Track B — Pricing Integration.
/// </summary>
public class PriceList
{
    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the tenant identifier this price list belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the display name of this price list.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the effective start date.</summary>
    public DateTime EffectiveFrom { get; private set; }

    /// <summary>Gets the effective end date (null = indefinite).</summary>
    public DateTime? EffectiveTo { get; private set; }

    /// <summary>Gets whether this price list is currently active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets the creation timestamp.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Gets the last update timestamp.</summary>
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<MaterialPricing> _materials = new();
    private readonly List<ComplexityModifier> _modifiers = new();

    /// <summary>Gets the material pricing rules for this price list.</summary>
    public IReadOnlyList<MaterialPricing> Materials => _materials.AsReadOnly();

    /// <summary>Gets the complexity modifiers for this price list.</summary>
    public IReadOnlyList<ComplexityModifier> Modifiers => _modifiers.AsReadOnly();

    /// <summary>Parameterless constructor for EF Core.</summary>
    private PriceList() { }

    /// <summary>
    /// Creates a new price list for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="name">The display name.</param>
    /// <param name="effectiveFrom">The effective start date.</param>
    /// <returns>A new price list instance.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public static PriceList Create(Guid tenantId, string name, DateTime effectiveFrom)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));

        if (effectiveFrom == default)
            throw new ArgumentException("EffectiveFrom must be set.", nameof(effectiveFrom));

        return new PriceList
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            EffectiveFrom = effectiveFrom,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Adds a material pricing rule to this price list.
    /// </summary>
    /// <param name="materialType">The material type (e.g., "MDF", "Plywood").</param>
    /// <param name="pricePerM2">Price per square meter.</param>
    /// <param name="currency">Currency code (e.g., "HUF").</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when material already exists.</exception>
    public void AddMaterial(string materialType, decimal pricePerM2, string currency = "HUF")
    {
        if (string.IsNullOrWhiteSpace(materialType))
            throw new ArgumentException("MaterialType cannot be null or empty.", nameof(materialType));

        if (pricePerM2 <= 0)
            throw new ArgumentException("PricePerM2 must be greater than zero.", nameof(pricePerM2));

        if (_materials.Any(m => m.MaterialType.Equals(materialType, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Material type '{materialType}' already exists in this price list.");

        var material = MaterialPricing.Create(Id, materialType, pricePerM2, currency);
        _materials.Add(material);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a complexity modifier to this price list.
    /// </summary>
    /// <param name="modifierType">The modifier type (e.g., "CutCount", "ShapeComplexity").</param>
    /// <param name="multiplierValue">The multiplier value (e.g., 0.10).</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when modifier already exists.</exception>
    public void AddComplexityModifier(string modifierType, decimal multiplierValue)
    {
        if (string.IsNullOrWhiteSpace(modifierType))
            throw new ArgumentException("ModifierType cannot be null or empty.", nameof(modifierType));

        if (multiplierValue < 0)
            throw new ArgumentException("MultiplierValue cannot be negative.", nameof(multiplierValue));

        if (_modifiers.Any(m => m.ModifierType.Equals(modifierType, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Modifier type '{modifierType}' already exists in this price list.");

        var modifier = ComplexityModifier.Create(Id, modifierType, multiplierValue);
        _modifiers.Add(modifier);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates this price list.
    /// </summary>
    /// <param name="effectiveTo">Optional end date (defaults to now).</param>
    public void Deactivate(DateTime? effectiveTo = null)
    {
        IsActive = false;
        EffectiveTo = effectiveTo ?? DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates this price list.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        EffectiveTo = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the price for a specific material type.
    /// </summary>
    /// <param name="materialType">The material type.</param>
    /// <returns>The material pricing, or null if not found.</returns>
    public MaterialPricing? GetMaterialPricing(string materialType)
    {
        return _materials.FirstOrDefault(m =>
            m.MaterialType.Equals(materialType, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the multiplier for a specific modifier type.
    /// </summary>
    /// <param name="modifierType">The modifier type.</param>
    /// <returns>The modifier value, or null if not found.</returns>
    public decimal? GetModifierValue(string modifierType)
    {
        return _modifiers.FirstOrDefault(m =>
            m.ModifierType.Equals(modifierType, StringComparison.OrdinalIgnoreCase))?.MultiplierValue;
    }
}
