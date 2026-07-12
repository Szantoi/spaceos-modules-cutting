using SpaceOS.Modules.Cutting.Domain.Common;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Events;
using SpaceOS.Modules.Cutting.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Domain.Aggregates;

/// <summary>
/// Aggregate root for customer quote requests (public, unauthenticated).
/// </summary>
public sealed class CuttingQuoteRequest : AggregateRoot
{
    /// <summary>
    /// Unique identifier for the quote request.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Tenant ID (organization that will process the quote).
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Human-readable quote number (e.g., QT-2026-001234).
    /// </summary>
    public string QuoteNumber { get; private set; } = string.Empty;

    /// <summary>
    /// Random tracking token for unauthenticated customer access.
    /// </summary>
    public string TrackingToken { get; private set; } = string.Empty;

    /// <summary>
    /// Customer contact information.
    /// </summary>
    public ContactInfo CustomerContact { get; private set; } = null!;

    private readonly List<QuoteLineItem> _items = new();
    /// <summary>
    /// Line items requested in the quote.
    /// </summary>
    public IReadOnlyList<QuoteLineItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Delivery details.
    /// </summary>
    public DeliveryDetails Delivery { get; private set; } = null!;

    /// <summary>
    /// Current status of the quote request (FSM state).
    /// </summary>
    public QuoteStatus Status { get; private set; }

    /// <summary>
    /// Quoted price (set when approved).
    /// </summary>
    public Money? QuotedPrice { get; private set; }

    /// <summary>
    /// Timestamp when the quote was reviewed.
    /// </summary>
    public DateTime? ReviewedAt { get; private set; }

    /// <summary>
    /// User ID who reviewed the quote.
    /// </summary>
    public Guid? ReviewedByUserId { get; private set; }

    /// <summary>
    /// Reason for rejection (if rejected).
    /// </summary>
    public string? RejectionReason { get; private set; }

    /// <summary>
    /// Timestamp when the quote was converted to an order.
    /// </summary>
    public DateTime? ConvertedToOrderAt { get; private set; }

    /// <summary>
    /// CuttingSheet ID created when quote was converted to order.
    /// </summary>
    public Guid? CuttingSheetId { get; private set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Concurrency control version.
    /// </summary>
    public int Version { get; private set; }

    // EF Core requires a parameterless constructor
    private CuttingQuoteRequest() { }

    /// <summary>
    /// Creates a new public quote request (unauthenticated submission).
    /// </summary>
    /// <param name="tenantId">Tenant ID that will process the quote.</param>
    /// <param name="quoteNumber">Human-readable quote number.</param>
    /// <param name="trackingToken">Random tracking token (12 chars hex).</param>
    /// <param name="customerContact">Customer contact information.</param>
    /// <param name="items">Quote line items.</param>
    /// <param name="delivery">Delivery details.</param>
    /// <returns>A new CuttingQuoteRequest in PendingReview status.</returns>
    public static CuttingQuoteRequest CreatePublic(
        Guid tenantId,
        string quoteNumber,
        string trackingToken,
        ContactInfo customerContact,
        IEnumerable<QuoteLineItem> items,
        DeliveryDetails delivery)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId required.", nameof(tenantId));

        ArgumentException.ThrowIfNullOrWhiteSpace(quoteNumber, nameof(quoteNumber));
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingToken, nameof(trackingToken));

        ArgumentNullException.ThrowIfNull(customerContact);
        ArgumentNullException.ThrowIfNull(delivery);

        customerContact.Validate();
        delivery.Validate();

        var itemList = items.ToList();
        if (itemList.Count == 0)
            throw new ArgumentException("Quote must have at least one item.", nameof(items));

        if (itemList.Count > 100)
            throw new ArgumentException("Quote cannot have more than 100 items.", nameof(items));

        foreach (var item in itemList)
        {
            item.Validate();
        }

        var now = DateTime.UtcNow;
        var quote = new CuttingQuoteRequest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            QuoteNumber = quoteNumber,
            TrackingToken = trackingToken,
            CustomerContact = customerContact,
            Delivery = delivery,
            Status = QuoteStatus.PendingReview,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1
        };

        quote._items.AddRange(itemList);

        quote.RaiseDomainEvent(new QuoteRequestSubmittedEvent(
            quote.Id,
            quote.QuoteNumber,
            customerContact.Email,
            customerContact.Name,
            itemList.Count,
            now));

        return quote;
    }

    /// <summary>
    /// Approves the quote request and sets the quoted price.
    /// Transitions status: PendingReview → Quoted.
    /// </summary>
    /// <param name="price">Quoted price.</param>
    /// <param name="userId">User ID who approved the quote.</param>
    public void ApproveAndQuote(Money price, Guid userId)
    {
        if (Status != QuoteStatus.PendingReview)
            throw new InvalidOperationException($"Cannot approve quote in status {Status}. Expected PendingReview.");

        ArgumentNullException.ThrowIfNull(price);
        price.Validate();

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId required.", nameof(userId));

        var now = DateTime.UtcNow;
        QuotedPrice = price;
        ReviewedAt = now;
        ReviewedByUserId = userId;
        Status = QuoteStatus.Quoted;
        UpdatedAt = now;
        Version++;

        RaiseDomainEvent(new QuoteApprovedEvent(
            Id,
            QuoteNumber,
            price.Amount,
            price.Currency,
            userId,
            now));
    }

    /// <summary>
    /// Rejects the quote request.
    /// Transitions status: PendingReview → Rejected.
    /// </summary>
    /// <param name="reason">Reason for rejection.</param>
    /// <param name="userId">User ID who rejected the quote.</param>
    public void Reject(string reason, Guid userId)
    {
        if (Status != QuoteStatus.PendingReview)
            throw new InvalidOperationException($"Cannot reject quote in status {Status}. Expected PendingReview.");

        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId required.", nameof(userId));

        var now = DateTime.UtcNow;
        RejectionReason = reason;
        ReviewedAt = now;
        ReviewedByUserId = userId;
        Status = QuoteStatus.Rejected;
        UpdatedAt = now;
        Version++;

        RaiseDomainEvent(new QuoteRejectedEvent(
            Id,
            QuoteNumber,
            reason,
            userId,
            now));
    }

    /// <summary>
    /// Converts the quote to a CuttingSheet order.
    /// Transitions status: Quoted → ConvertedToOrder.
    /// </summary>
    /// <param name="cuttingSheetId">ID of the created CuttingSheet.</param>
    public void ConvertToOrder(Guid cuttingSheetId)
    {
        if (Status != QuoteStatus.Quoted)
            throw new InvalidOperationException($"Cannot convert quote in status {Status}. Expected Quoted.");

        if (cuttingSheetId == Guid.Empty)
            throw new ArgumentException("CuttingSheetId required.", nameof(cuttingSheetId));

        var now = DateTime.UtcNow;
        CuttingSheetId = cuttingSheetId;
        ConvertedToOrderAt = now;
        Status = QuoteStatus.ConvertedToOrder;
        UpdatedAt = now;
        Version++;

        RaiseDomainEvent(new QuoteConvertedToOrderEvent(
            Id,
            QuoteNumber,
            cuttingSheetId,
            now));
    }
}
