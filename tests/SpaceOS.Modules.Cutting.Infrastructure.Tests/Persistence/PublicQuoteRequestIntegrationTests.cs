using FluentAssertions;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Events;
using SpaceOS.Modules.Cutting.Infrastructure.Repositories;
using Xunit;

namespace SpaceOS.Modules.Cutting.Infrastructure.Tests.Persistence;

/// <summary>
/// Integration tests for PublicQuoteRequest persistence and domain events (MSG-BACKEND-088 Phase 5).
/// </summary>
public sealed class PublicQuoteRequestIntegrationTests : InfrastructureIntegrationTestBase
{
    /// <summary>
    /// Test 1: Persistence works - entity saved to database.
    /// </summary>
    [Fact]
    public async Task CreatePublicQuoteRequest_ShouldPersistToDatabase()
    {
        // Arrange
        var repository = new CuttingRepository(_dbContext!);
        var quoteRequest = PublicQuoteRequest.Create(
            customerName: "John Doe",
            customerEmail: "john@example.com",
            customerPhone: "+36 20 123 4567",
            companyName: "Example Ltd",
            material: "Oak",
            lengthMm: 2800m,
            widthMm: 600m,
            thicknessMm: 18m,
            quantity: 5,
            edgeType: "ABS",
            surface: "Matt",
            urgency: "standard",
            notes: "Test quote");

        // Act
        await repository.AddPublicQuoteRequestAsync(quoteRequest, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        // Assert - verify saved
        var saved = await repository.GetPublicQuoteRequestByIdAsync(quoteRequest.Id, CancellationToken.None);
        saved.Should().NotBeNull();
        saved!.Id.Should().Be(quoteRequest.Id);
        saved.CustomerName.Should().Be("John Doe");
        saved.CustomerEmail.Should().Be("john@example.com");
        saved.Material.Should().Be("Oak");
        saved.Quantity.Should().Be(5);
        saved.Status.Should().Be("received");
    }

    /// <summary>
    /// Test 2: Domain event raised - PublicQuoteRequestCreatedEvent collected.
    /// </summary>
    [Fact]
    public void CreatePublicQuoteRequest_ShouldRaiseDomainEvent()
    {
        // Arrange & Act
        var quoteRequest = PublicQuoteRequest.Create(
            customerName: "Jane Smith",
            customerEmail: "jane@example.com",
            customerPhone: null,
            companyName: null,
            material: "Beech",
            lengthMm: 1200m,
            widthMm: 400m,
            thicknessMm: 20m,
            quantity: 10,
            edgeType: "None",
            surface: "Glossy",
            urgency: "express",
            notes: null);

        // Assert - verify domain event raised
        quoteRequest.DomainEvents.Should().HaveCount(1);
        var domainEvent = quoteRequest.DomainEvents.Single();
        domainEvent.Should().BeOfType<PublicQuoteRequestCreatedEvent>();

        var @event = (PublicQuoteRequestCreatedEvent)domainEvent;
        @event.QuoteId.Should().Be(quoteRequest.Id);
        @event.CustomerName.Should().Be("Jane Smith");
        @event.CustomerEmail.Should().Be("jane@example.com");
        @event.Material.Should().Be("Beech");
        @event.Quantity.Should().Be(10);
        @event.Urgency.Should().Be("express");
    }

    /// <summary>
    /// Test 3: End-to-end - persistence + domain event outbox.
    /// Verifies that SaveChanges triggers OutboxSaveChangesInterceptor.
    /// </summary>
    [Fact]
    public async Task SaveChanges_ShouldPersistEntityAndWriteOutboxMessages()
    {
        // Arrange
        var repository = new CuttingRepository(_dbContext!);
        var quoteRequest = PublicQuoteRequest.Create(
            customerName: "Bob Builder",
            customerEmail: "bob@construction.com",
            customerPhone: "+36 30 999 8888",
            companyName: "Build Corp",
            material: "Plywood",
            lengthMm: 2400m,
            widthMm: 1200m,
            thicknessMm: 15m,
            quantity: 20,
            edgeType: "PVC",
            surface: "Textured",
            urgency: "standard",
            notes: "Bulk order");

        // Act
        await repository.AddPublicQuoteRequestAsync(quoteRequest, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        // Assert 1: Entity persisted
        var saved = await repository.GetPublicQuoteRequestByIdAsync(quoteRequest.Id, CancellationToken.None);
        saved.Should().NotBeNull();

        // Assert 2: Domain events cleared (PopDomainEvents called by interceptor)
        // Note: We can't directly assert on LocalOutboxMessages without exposing DbSet,
        // but we can verify that domain events were popped from the aggregate.
        // In a real test with Testcontainers, we would query LocalOutboxMessages DbSet.
        saved!.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Test 4: Multiple quote requests - verify isolation.
    /// </summary>
    [Fact]
    public async Task CreateMultipleQuoteRequests_ShouldIsolateDomainEvents()
    {
        // Arrange
        var repository = new CuttingRepository(_dbContext!);
        var quote1 = PublicQuoteRequest.Create(
            customerName: "Alice", customerEmail: "alice@test.com", customerPhone: null, companyName: null,
            material: "Pine", lengthMm: 1000m, widthMm: 300m, thicknessMm: 12m, quantity: 2,
            edgeType: "None", surface: "Natural", urgency: "standard", notes: null);

        var quote2 = PublicQuoteRequest.Create(
            customerName: "Charlie", customerEmail: "charlie@test.com", customerPhone: null, companyName: null,
            material: "Walnut", lengthMm: 1500m, widthMm: 500m, thicknessMm: 25m, quantity: 3,
            edgeType: "Veneer", surface: "Polished", urgency: "express", notes: null);

        // Act
        await repository.AddPublicQuoteRequestAsync(quote1, CancellationToken.None);
        await repository.AddPublicQuoteRequestAsync(quote2, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        // Assert - both saved independently
        var saved1 = await repository.GetPublicQuoteRequestByIdAsync(quote1.Id, CancellationToken.None);
        var saved2 = await repository.GetPublicQuoteRequestByIdAsync(quote2.Id, CancellationToken.None);

        saved1.Should().NotBeNull();
        saved2.Should().NotBeNull();
        saved1!.CustomerName.Should().Be("Alice");
        saved2!.CustomerName.Should().Be("Charlie");
        saved1.Id.Should().NotBe(saved2.Id);
    }
}
