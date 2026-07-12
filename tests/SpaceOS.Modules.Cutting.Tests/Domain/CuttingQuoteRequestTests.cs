using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Domain;

/// <summary>
/// Unit tests for CuttingQuoteRequest aggregate.
/// </summary>
public class CuttingQuoteRequestTests
{
    [Fact]
    public void CreatePublic_ValidData_ShouldCreateQuoteInPendingReviewStatus()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var quoteNumber = "QT-2026-001234";
        var trackingToken = "a8f3d9e2b1c4";
        var contact = new ContactInfo("test@example.com", "Test User", "+36301234567");
        var items = new List<QuoteLineItem>
        {
            new QuoteLineItem(MaterialType.MDF_18MM, 2800, 2070, 5, EdgingType.ABS_2MM_WHITE, "Test note")
        };
        var delivery = new DeliveryDetails("Budapest, Kossuth utca 10.", DateTime.UtcNow.AddDays(7));

        // Act
        var quote = CuttingQuoteRequest.CreatePublic(tenantId, quoteNumber, trackingToken, contact, items, delivery);

        // Assert
        Assert.NotEqual(Guid.Empty, quote.Id);
        Assert.Equal(tenantId, quote.TenantId);
        Assert.Equal(quoteNumber, quote.QuoteNumber);
        Assert.Equal(trackingToken, quote.TrackingToken);
        Assert.Equal(QuoteStatus.PendingReview, quote.Status);
        Assert.Single(quote.Items);
        Assert.Single(quote.DomainEvents);
    }

    [Fact]
    public void CreatePublic_EmptyTenantId_ShouldThrowArgumentException()
    {
        // Arrange
        var contact = new ContactInfo("test@example.com", "Test User", null);
        var items = new List<QuoteLineItem>
        {
            new QuoteLineItem(MaterialType.MDF_18MM, 2800, 2070, 5, EdgingType.None, null)
        };
        var delivery = new DeliveryDetails("Test Address", null);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            CuttingQuoteRequest.CreatePublic(Guid.Empty, "QT-001", "token", contact, items, delivery));
    }

    [Fact]
    public void CreatePublic_NoItems_ShouldThrowArgumentException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var contact = new ContactInfo("test@example.com", "Test User", null);
        var delivery = new DeliveryDetails("Test Address", null);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            CuttingQuoteRequest.CreatePublic(tenantId, "QT-001", "token", contact, Array.Empty<QuoteLineItem>(), delivery));
    }

    [Fact]
    public void ApproveAndQuote_FromPendingReview_ShouldTransitionToQuotedStatus()
    {
        // Arrange
        var quote = CreateValidQuote();
        var userId = Guid.NewGuid();
        var price = new Money(45000m, "HUF");

        // Act
        quote.ApproveAndQuote(price, userId);

        // Assert
        Assert.Equal(QuoteStatus.Quoted, quote.Status);
        Assert.NotNull(quote.QuotedPrice);
        Assert.Equal(45000m, quote.QuotedPrice.Amount);
        Assert.Equal("HUF", quote.QuotedPrice.Currency);
        Assert.Equal(userId, quote.ReviewedByUserId);
        Assert.NotNull(quote.ReviewedAt);
        Assert.Equal(2, quote.DomainEvents.Count); // CreatePublic + ApproveAndQuote
    }

    [Fact]
    public void ApproveAndQuote_FromQuotedStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var quote = CreateValidQuote();
        var userId = Guid.NewGuid();
        var price = new Money(45000m, "HUF");
        quote.ApproveAndQuote(price, userId); // First approval

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            quote.ApproveAndQuote(new Money(50000m, "HUF"), userId)); // Second approval should fail
    }

    [Fact]
    public void Reject_FromPendingReview_ShouldTransitionToRejectedStatus()
    {
        // Arrange
        var quote = CreateValidQuote();
        var userId = Guid.NewGuid();
        var reason = "Insufficient capacity";

        // Act
        quote.Reject(reason, userId);

        // Assert
        Assert.Equal(QuoteStatus.Rejected, quote.Status);
        Assert.Equal(reason, quote.RejectionReason);
        Assert.Equal(userId, quote.ReviewedByUserId);
        Assert.NotNull(quote.ReviewedAt);
        Assert.Equal(2, quote.DomainEvents.Count);
    }

    [Fact]
    public void Reject_FromQuotedStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var quote = CreateValidQuote();
        var userId = Guid.NewGuid();
        quote.ApproveAndQuote(new Money(45000m, "HUF"), userId);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            quote.Reject("Cannot reject quoted quote", userId));
    }

    [Fact]
    public void ConvertToOrder_FromQuotedStatus_ShouldTransitionToConvertedToOrderStatus()
    {
        // Arrange
        var quote = CreateValidQuote();
        var userId = Guid.NewGuid();
        quote.ApproveAndQuote(new Money(45000m, "HUF"), userId);
        var cuttingSheetId = Guid.NewGuid();

        // Act
        quote.ConvertToOrder(cuttingSheetId);

        // Assert
        Assert.Equal(QuoteStatus.ConvertedToOrder, quote.Status);
        Assert.Equal(cuttingSheetId, quote.CuttingSheetId);
        Assert.NotNull(quote.ConvertedToOrderAt);
        Assert.Equal(3, quote.DomainEvents.Count);
    }

    [Fact]
    public void ConvertToOrder_FromPendingReviewStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var quote = CreateValidQuote();
        var cuttingSheetId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            quote.ConvertToOrder(cuttingSheetId));
    }

    [Fact]
    public void ContactInfo_Validate_InvalidEmail_ShouldThrowArgumentException()
    {
        // Arrange
        var contact = new ContactInfo("invalid-email", "Test User", null);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => contact.Validate());
    }

    [Fact]
    public void QuoteLineItem_Validate_InvalidWidth_ShouldThrowArgumentException()
    {
        // Arrange
        var item = new QuoteLineItem(MaterialType.MDF_18MM, 0, 2070, 5, EdgingType.None, null);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => item.Validate());
    }

    [Fact]
    public void QuoteLineItem_Validate_WidthTooLarge_ShouldThrowArgumentException()
    {
        // Arrange
        var item = new QuoteLineItem(MaterialType.MDF_18MM, 10000, 2070, 5, EdgingType.None, null);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => item.Validate());
    }

    [Fact]
    public void Money_Validate_NegativeAmount_ShouldThrowArgumentException()
    {
        // Arrange
        var money = new Money(-100m, "HUF");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => money.Validate());
    }

    [Fact]
    public void Money_Validate_InvalidCurrencyCode_ShouldThrowArgumentException()
    {
        // Arrange
        var money = new Money(100m, "INVALID");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => money.Validate());
    }

    [Fact]
    public void DeliveryDetails_Validate_PastDate_ShouldThrowArgumentException()
    {
        // Arrange
        var delivery = new DeliveryDetails("Test Address", DateTime.UtcNow.AddDays(-7));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => delivery.Validate());
    }

    // Helper method
    private static CuttingQuoteRequest CreateValidQuote()
    {
        var tenantId = Guid.NewGuid();
        var quoteNumber = "QT-2026-001234";
        var trackingToken = "a8f3d9e2b1c4";
        var contact = new ContactInfo("test@example.com", "Test User", "+36301234567");
        var items = new List<QuoteLineItem>
        {
            new QuoteLineItem(MaterialType.MDF_18MM, 2800, 2070, 5, EdgingType.ABS_2MM_WHITE, "Test note")
        };
        var delivery = new DeliveryDetails("Budapest, Kossuth utca 10.", DateTime.UtcNow.AddDays(7));

        return CuttingQuoteRequest.CreatePublic(tenantId, quoteNumber, trackingToken, contact, items, delivery);
    }
}
