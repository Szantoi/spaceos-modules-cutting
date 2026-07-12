namespace SpaceOS.Modules.Cutting.Domain.ValueObjects;

/// <summary>
/// Customer contact information for quote requests.
/// </summary>
/// <param name="Email">Customer email address (required).</param>
/// <param name="Name">Customer full name (required).</param>
/// <param name="Phone">Customer phone number (optional).</param>
public record ContactInfo(string Email, string Name, string? Phone)
{
    /// <summary>
    /// Validates contact information.
    /// </summary>
    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Email, nameof(Email));
        ArgumentException.ThrowIfNullOrWhiteSpace(Name, nameof(Name));

        if (!Email.Contains('@'))
            throw new ArgumentException("Invalid email address.", nameof(Email));
    }
}
