namespace SpaceOS.Modules.Cutting.Domain.ValueObjects;

/// <summary>
/// Delivery information for quote request.
/// </summary>
/// <param name="Address">Delivery address (required).</param>
/// <param name="RequestedDate">Requested delivery date (optional).</param>
public record DeliveryDetails(string Address, DateTime? RequestedDate)
{
    /// <summary>
    /// Validates delivery details.
    /// </summary>
    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Address, nameof(Address));

        if (RequestedDate.HasValue && RequestedDate.Value < DateTime.UtcNow.Date)
            throw new ArgumentException("Requested delivery date cannot be in the past.", nameof(RequestedDate));
    }
}
