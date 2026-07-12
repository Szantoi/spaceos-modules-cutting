using SpaceOS.Modules.Cutting.Domain.Common;
using SpaceOS.Modules.Cutting.Domain.Events;

namespace SpaceOS.Modules.Cutting.Domain.Entities;

/// <summary>
/// Public quote request aggregate root for B2C customers (Q3 Track A - MSG-BACKEND-030).
/// Simplified single-item quote format without tenant resolution.
/// </summary>
public sealed class PublicQuoteRequest : AggregateRoot
{
    public Guid Id { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = string.Empty;
    public string? CustomerPhone { get; private set; }
    public string? CompanyName { get; private set; }
    public string Material { get; private set; } = string.Empty;
    public decimal LengthMm { get; private set; }
    public decimal WidthMm { get; private set; }
    public decimal ThicknessMm { get; private set; }
    public int Quantity { get; private set; }
    public string EdgeType { get; private set; } = string.Empty;
    public string Surface { get; private set; } = string.Empty;
    public string Urgency { get; private set; } = "standard";
    public string? Notes { get; private set; }
    public string Status { get; private set; } = "received";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core requires a parameterless constructor
    private PublicQuoteRequest() { }

    /// <summary>
    /// Creates a new public quote request.
    /// </summary>
    public static PublicQuoteRequest Create(
        string customerName,
        string customerEmail,
        string? customerPhone,
        string? companyName,
        string material,
        decimal lengthMm,
        decimal widthMm,
        decimal thicknessMm,
        int quantity,
        string edgeType,
        string surface,
        string urgency,
        string? notes)
    {
        var now = DateTime.UtcNow;

        var quoteRequest = new PublicQuoteRequest
        {
            Id = Guid.NewGuid(),
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            CustomerPhone = customerPhone,
            CompanyName = companyName,
            Material = material,
            LengthMm = lengthMm,
            WidthMm = widthMm,
            ThicknessMm = thicknessMm,
            Quantity = quantity,
            EdgeType = edgeType,
            Surface = surface,
            Urgency = urgency,
            Notes = notes,
            Status = "received",
            CreatedAt = now,
            UpdatedAt = now
        };

        // Raise domain event (Phase 4 - MSG-BACKEND-088)
        quoteRequest.RaiseDomainEvent(new PublicQuoteRequestCreatedEvent(
            QuoteId: quoteRequest.Id,
            CustomerName: quoteRequest.CustomerName,
            CustomerEmail: quoteRequest.CustomerEmail,
            CustomerPhone: quoteRequest.CustomerPhone,
            Material: quoteRequest.Material,
            Quantity: quoteRequest.Quantity,
            Urgency: quoteRequest.Urgency,
            CreatedAt: quoteRequest.CreatedAt));

        return quoteRequest;
    }

    /// <summary>
    /// Updates the status of the quote request.
    /// </summary>
    public void UpdateStatus(string newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}
