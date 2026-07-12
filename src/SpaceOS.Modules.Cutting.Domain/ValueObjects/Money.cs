namespace SpaceOS.Modules.Cutting.Domain.ValueObjects;

/// <summary>
/// Represents a monetary value with currency.
/// </summary>
/// <param name="Amount">The monetary amount.</param>
/// <param name="Currency">Currency code (e.g., HUF, EUR, USD).</param>
public record Money(decimal Amount, string Currency)
{
    /// <summary>
    /// Validates money value.
    /// </summary>
    public void Validate()
    {
        if (Amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(Amount));

        ArgumentException.ThrowIfNullOrWhiteSpace(Currency, nameof(Currency));

        if (Currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(Currency));
    }
}
