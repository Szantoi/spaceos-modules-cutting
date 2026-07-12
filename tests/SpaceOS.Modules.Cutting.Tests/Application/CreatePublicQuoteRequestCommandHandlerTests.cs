using Ardalis.Result;
using FluentAssertions;
using Moq;
using SpaceOS.Modules.Cutting.Application.Commands.CreatePublicQuoteRequest;
using SpaceOS.Modules.Cutting.Application.DTOs.QuoteRequest;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Application;

/// <summary>
/// Unit tests for CreatePublicQuoteRequestCommandHandler (MSG-BACKEND-078 Phase 4)
/// </summary>
public class CreatePublicQuoteRequestCommandHandlerTests
{
    private readonly Mock<ICuttingRepository> _repoMock = new();

    private CreatePublicQuoteRequestCommandHandler CreateHandler()
        => new(_repoMock.Object);

    // ── 1. Happy path: valid request → 201 response ──────────────────────────

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessWithQuoteId()
    {
        // Arrange
        var dto = new PublicQuoteRequestDto
        {
            CustomerName = "John Doe",
            CustomerEmail = "john.doe@example.com",
            CustomerPhone = "+36301234567",
            CompanyName = "Test Company",
            Material = "MDF 18mm",
            Dimensions = new DimensionsDto { Length = 600, Width = 400, Thickness = 18 },
            Quantity = 5,
            EdgeType = "ABS 2mm",
            Surface = "Painted White",
            Urgency = "standard",
            Notes = "Test quote request"
        };

        var handler = CreateHandler();
        var command = new CreatePublicQuoteRequestCommand { Data = dto };

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.QuoteId.Should().NotBeEmpty();
        result.Value.Status.Should().Be("received");
        result.Value.EstimatedReplyTime.Should().Be("2 business days");
        result.Value.TrackingUrl.Should().StartWith("/public/quote/");

        _repoMock.Verify(r => r.AddPublicQuoteRequestAsync(
            It.IsAny<PublicQuoteRequest>(),
            default), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    // ── 2. Express urgency → 1 business day ──────────────────────────────────

    [Fact]
    public async Task Handle_ExpressUrgency_Returns1BusinessDay()
    {
        // Arrange
        var dto = new PublicQuoteRequestDto
        {
            CustomerName = "Jane Smith",
            CustomerEmail = "jane@example.com",
            Material = "HDF 3mm",
            Dimensions = new DimensionsDto { Length = 500, Width = 300, Thickness = 3 },
            Quantity = 10,
            EdgeType = "None",
            Surface = "Natural",
            Urgency = "express"
        };

        var handler = CreateHandler();
        var command = new CreatePublicQuoteRequestCommand { Data = dto };

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EstimatedReplyTime.Should().Be("1 business day");
    }

    // ── 3. Standard urgency → 2 business days ────────────────────────────────

    [Fact]
    public async Task Handle_StandardUrgency_Returns2BusinessDays()
    {
        // Arrange
        var dto = new PublicQuoteRequestDto
        {
            CustomerName = "Bob Wilson",
            CustomerEmail = "bob@example.com",
            Material = "Plywood 12mm",
            Dimensions = new DimensionsDto { Length = 1200, Width = 800, Thickness = 12 },
            Quantity = 3,
            EdgeType = "PVC",
            Surface = "Veneered",
            Urgency = "standard"
        };

        var handler = CreateHandler();
        var command = new CreatePublicQuoteRequestCommand { Data = dto };

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EstimatedReplyTime.Should().Be("2 business days");
    }

    // ── 4. Urgency case-insensitive ──────────────────────────────────────────

    [Theory]
    [InlineData("EXPRESS")]
    [InlineData("Express")]
    [InlineData("ExPrEsS")]
    public async Task Handle_ExpressUrgency_IsCaseInsensitive(string urgency)
    {
        // Arrange
        var dto = new PublicQuoteRequestDto
        {
            CustomerName = "Test User",
            CustomerEmail = "test@example.com",
            Material = "MDF 18mm",
            Dimensions = new DimensionsDto { Length = 600, Width = 400, Thickness = 18 },
            Quantity = 1,
            EdgeType = "ABS",
            Surface = "Painted",
            Urgency = urgency
        };

        var handler = CreateHandler();
        var command = new CreatePublicQuoteRequestCommand { Data = dto };

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.Value.EstimatedReplyTime.Should().Be("1 business day");
    }

    // ── 5. Tracking URL format ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidRequest_ReturnsCorrectTrackingUrl()
    {
        // Arrange
        var dto = new PublicQuoteRequestDto
        {
            CustomerName = "Alice Johnson",
            CustomerEmail = "alice@example.com",
            Material = "Chipboard 16mm",
            Dimensions = new DimensionsDto { Length = 800, Width = 600, Thickness = 16 },
            Quantity = 2,
            EdgeType = "Melamine",
            Surface = "Laminated",
            Urgency = "standard"
        };

        var handler = CreateHandler();
        var command = new CreatePublicQuoteRequestCommand { Data = dto };

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.Value.TrackingUrl.Should().MatchRegex(@"^/public/quote/[\da-f-]{36}/status$");
    }

    // ── 6. Persistence verification ───────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidRequest_CallsRepositoryMethods()
    {
        // Arrange
        var dto = new PublicQuoteRequestDto
        {
            CustomerName = "Test Persistence",
            CustomerEmail = "persist@example.com",
            Material = "MDF 18mm",
            Dimensions = new DimensionsDto { Length = 600, Width = 400, Thickness = 18 },
            Quantity = 1,
            EdgeType = "ABS",
            Surface = "Painted",
            Urgency = "standard"
        };

        PublicQuoteRequest? capturedEntity = null;
        _repoMock.Setup(r => r.AddPublicQuoteRequestAsync(It.IsAny<PublicQuoteRequest>(), default))
            .Callback<PublicQuoteRequest, CancellationToken>((entity, _) => capturedEntity = entity)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = new CreatePublicQuoteRequestCommand { Data = dto };

        // Act
        await handler.Handle(command, default);

        // Assert
        capturedEntity.Should().NotBeNull();
        capturedEntity!.CustomerName.Should().Be("Test Persistence");
        capturedEntity.CustomerEmail.Should().Be("persist@example.com");
        capturedEntity.Material.Should().Be("MDF 18mm");
        capturedEntity.LengthMm.Should().Be(600);
        capturedEntity.WidthMm.Should().Be(400);
        capturedEntity.ThicknessMm.Should().Be(18);
        capturedEntity.Quantity.Should().Be(1);
        capturedEntity.Status.Should().Be("received");

        _repoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    // ── 7. All fields mapped correctly ────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidRequest_MapsAllFieldsCorrectly()
    {
        // Arrange
        var dto = new PublicQuoteRequestDto
        {
            CustomerName = "Complete Mapping Test",
            CustomerEmail = "mapping@test.com",
            CustomerPhone = "+36309876543",
            CompanyName = "Mapping Ltd.",
            Material = "Plywood 15mm",
            Dimensions = new DimensionsDto { Length = 1500, Width = 900, Thickness = 15 },
            Quantity = 7,
            EdgeType = "Veneer",
            Surface = "Oil Finish",
            Urgency = "express",
            Notes = "Special requirements here"
        };

        PublicQuoteRequest? capturedEntity = null;
        _repoMock.Setup(r => r.AddPublicQuoteRequestAsync(It.IsAny<PublicQuoteRequest>(), default))
            .Callback<PublicQuoteRequest, CancellationToken>((entity, _) => capturedEntity = entity)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = new CreatePublicQuoteRequestCommand { Data = dto };

        // Act
        await handler.Handle(command, default);

        // Assert
        capturedEntity.Should().NotBeNull();
        capturedEntity!.CustomerName.Should().Be("Complete Mapping Test");
        capturedEntity.CustomerEmail.Should().Be("mapping@test.com");
        capturedEntity.CustomerPhone.Should().Be("+36309876543");
        capturedEntity.CompanyName.Should().Be("Mapping Ltd.");
        capturedEntity.Material.Should().Be("Plywood 15mm");
        capturedEntity.LengthMm.Should().Be(1500);
        capturedEntity.WidthMm.Should().Be(900);
        capturedEntity.ThicknessMm.Should().Be(15);
        capturedEntity.Quantity.Should().Be(7);
        capturedEntity.EdgeType.Should().Be("Veneer");
        capturedEntity.Surface.Should().Be("Oil Finish");
        capturedEntity.Urgency.Should().Be("express");
        capturedEntity.Notes.Should().Be("Special requirements here");
    }

    // ── 8. Optional fields (phone, company, notes) ────────────────────────────

    [Fact]
    public async Task Handle_OptionalFieldsNull_HandledCorrectly()
    {
        // Arrange
        var dto = new PublicQuoteRequestDto
        {
            CustomerName = "Minimal Fields",
            CustomerEmail = "minimal@test.com",
            CustomerPhone = null,  // optional
            CompanyName = null,    // optional
            Material = "MDF 18mm",
            Dimensions = new DimensionsDto { Length = 600, Width = 400, Thickness = 18 },
            Quantity = 1,
            EdgeType = "ABS",
            Surface = "Painted",
            Urgency = "standard",
            Notes = null  // optional
        };

        PublicQuoteRequest? capturedEntity = null;
        _repoMock.Setup(r => r.AddPublicQuoteRequestAsync(It.IsAny<PublicQuoteRequest>(), default))
            .Callback<PublicQuoteRequest, CancellationToken>((entity, _) => capturedEntity = entity)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = new CreatePublicQuoteRequestCommand { Data = dto };

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedEntity.Should().NotBeNull();
        capturedEntity!.CustomerPhone.Should().BeNull();
        capturedEntity.CompanyName.Should().BeNull();
        capturedEntity.Notes.Should().BeNull();
    }
}
