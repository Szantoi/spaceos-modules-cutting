using Ardalis.Result;
using Moq;
using SpaceOS.Modules.Cutting.Application.Commands.CreatePublicQuoteRequest;
using SpaceOS.Modules.Cutting.Application.DTOs.QuoteRequest;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Tests.Application.Commands;

/// <summary>
/// Unit tests for CreatePublicQuoteRequestCommandHandler (MSG-BACKEND-079 Phase 4).
/// Tests the public quote request creation handler without database dependencies.
/// </summary>
public class CreatePublicQuoteRequestCommandHandlerTests
{
    private readonly Mock<ICuttingRepository> _mockRepository;
    private readonly CreatePublicQuoteRequestCommandHandler _handler;

    public CreatePublicQuoteRequestCommandHandlerTests()
    {
        _mockRepository = new Mock<ICuttingRepository>();
        _handler = new CreatePublicQuoteRequestCommandHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_CreatesQuoteRequestAndReturnsSuccess()
    {
        // Arrange
        var command = new CreatePublicQuoteRequestCommand
        {
            Data = new PublicQuoteRequestDto
            {
                CustomerName = "Test Customer",
                CustomerEmail = "customer@example.com",
                CustomerPhone = "+36301234567",
                CompanyName = "Test Company Ltd.",
                Material = "Oak",
                Dimensions = new DimensionsDto
                {
                    Length = 2440,
                    Width = 1220,
                    Thickness = 18
                },
                Quantity = 5,
                EdgeType = "PVC",
                Surface = "Laminated",
                Urgency = "standard",
                Notes = "Test notes"
            }
        };

        _mockRepository
            .Setup(r => r.AddPublicQuoteRequestAsync(It.IsAny<PublicQuoteRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEqual(Guid.Empty, result.Value.QuoteId);
        Assert.Equal("received", result.Value.Status);
        Assert.Equal("2 business days", result.Value.EstimatedReplyTime);
        Assert.Contains("/public/quote/", result.Value.TrackingUrl);

        _mockRepository.Verify(
            r => r.AddPublicQuoteRequestAsync(
                It.Is<PublicQuoteRequest>(q =>
                    q.CustomerName == command.Data.CustomerName &&
                    q.CustomerEmail == command.Data.CustomerEmail &&
                    q.Material == command.Data.Material &&
                    q.LengthMm == command.Data.Dimensions.Length &&
                    q.WidthMm == command.Data.Dimensions.Width &&
                    q.ThicknessMm == command.Data.Dimensions.Thickness &&
                    q.Quantity == command.Data.Quantity &&
                    q.EdgeType == command.Data.EdgeType &&
                    q.Surface == command.Data.Surface &&
                    q.Urgency == command.Data.Urgency
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );

        _mockRepository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WithExpressUrgency_ReturnsOneBusinessDayEstimate()
    {
        // Arrange
        var command = new CreatePublicQuoteRequestCommand
        {
            Data = new PublicQuoteRequestDto
            {
                CustomerName = "Express Customer",
                CustomerEmail = "express@example.com",
                Material = "Pine",
                Dimensions = new DimensionsDto
                {
                    Length = 2000,
                    Width = 1000,
                    Thickness = 16
                },
                Quantity = 10,
                EdgeType = "None",
                Surface = "Raw",
                Urgency = "express"
            }
        };

        _mockRepository
            .Setup(r => r.AddPublicQuoteRequestAsync(It.IsAny<PublicQuoteRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("1 business day", result.Value!.EstimatedReplyTime);
    }

    [Fact]
    public async Task Handle_WithMinimalRequest_CreatesQuoteRequestWithDefaults()
    {
        // Arrange - Only required fields
        var command = new CreatePublicQuoteRequestCommand
        {
            Data = new PublicQuoteRequestDto
            {
                CustomerName = "Minimal Customer",
                CustomerEmail = "minimal@example.com",
                Material = "Plywood",
                Dimensions = new DimensionsDto
                {
                    Length = 1500,
                    Width = 800,
                    Thickness = 12
                },
                Quantity = 1,
                EdgeType = "None",
                Surface = "Raw",
                Urgency = "standard"
            }
        };

        _mockRepository
            .Setup(r => r.AddPublicQuoteRequestAsync(It.IsAny<PublicQuoteRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        _mockRepository.Verify(
            r => r.AddPublicQuoteRequestAsync(
                It.Is<PublicQuoteRequest>(q =>
                    q.CustomerName == "Minimal Customer" &&
                    q.CustomerEmail == "minimal@example.com" &&
                    q.CustomerPhone == null &&
                    q.CompanyName == null &&
                    q.Notes == null
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WithRepositoryFailure_PropagatesException()
    {
        // Arrange
        var command = new CreatePublicQuoteRequestCommand
        {
            Data = new PublicQuoteRequestDto
            {
                CustomerName = "Test",
                CustomerEmail = "test@example.com",
                Material = "Oak",
                Dimensions = new DimensionsDto { Length = 2000, Width = 1000, Thickness = 18 },
                Quantity = 1,
                EdgeType = "None",
                Surface = "Raw",
                Urgency = "standard"
            }
        };

        _mockRepository
            .Setup(r => r.AddPublicQuoteRequestAsync(It.IsAny<PublicQuoteRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Handle_WithSaveChangesFailure_PropagatesException()
    {
        // Arrange
        var command = new CreatePublicQuoteRequestCommand
        {
            Data = new PublicQuoteRequestDto
            {
                CustomerName = "Test",
                CustomerEmail = "test@example.com",
                Material = "Oak",
                Dimensions = new DimensionsDto { Length = 2000, Width = 1000, Thickness = 18 },
                Quantity = 1,
                EdgeType = "None",
                Surface = "Raw",
                Urgency = "standard"
            }
        };

        _mockRepository
            .Setup(r => r.AddPublicQuoteRequestAsync(It.IsAny<PublicQuoteRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Transaction failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Handle_GeneratesUniqueQuoteId()
    {
        // Arrange
        var command1 = new CreatePublicQuoteRequestCommand
        {
            Data = new PublicQuoteRequestDto
            {
                CustomerName = "Customer 1",
                CustomerEmail = "customer1@example.com",
                Material = "Oak",
                Dimensions = new DimensionsDto { Length = 2000, Width = 1000, Thickness = 18 },
                Quantity = 1,
                EdgeType = "None",
                Surface = "Raw",
                Urgency = "standard"
            }
        };

        var command2 = new CreatePublicQuoteRequestCommand
        {
            Data = new PublicQuoteRequestDto
            {
                CustomerName = "Customer 2",
                CustomerEmail = "customer2@example.com",
                Material = "Pine",
                Dimensions = new DimensionsDto { Length = 2000, Width = 1000, Thickness = 18 },
                Quantity = 1,
                EdgeType = "None",
                Surface = "Raw",
                Urgency = "standard"
            }
        };

        _mockRepository
            .Setup(r => r.AddPublicQuoteRequestAsync(It.IsAny<PublicQuoteRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result1 = await _handler.Handle(command1, CancellationToken.None);
        var result2 = await _handler.Handle(command2, CancellationToken.None);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotEqual(result1.Value!.QuoteId, result2.Value!.QuoteId);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CreatePublicQuoteRequestCommandHandler(null!));
    }

    [Fact]
    public async Task Handle_CreatesEntityWithCorrectTimestamps()
    {
        // Arrange
        var command = new CreatePublicQuoteRequestCommand
        {
            Data = new PublicQuoteRequestDto
            {
                CustomerName = "Test",
                CustomerEmail = "test@example.com",
                Material = "Oak",
                Dimensions = new DimensionsDto { Length = 2000, Width = 1000, Thickness = 18 },
                Quantity = 1,
                EdgeType = "None",
                Surface = "Raw",
                Urgency = "standard"
            }
        };

        PublicQuoteRequest? capturedEntity = null;

        _mockRepository
            .Setup(r => r.AddPublicQuoteRequestAsync(It.IsAny<PublicQuoteRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PublicQuoteRequest, CancellationToken>((entity, ct) => capturedEntity = entity)
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var before = DateTime.UtcNow;
        await _handler.Handle(command, CancellationToken.None);
        var after = DateTime.UtcNow;

        // Assert
        Assert.NotNull(capturedEntity);
        Assert.InRange(capturedEntity.CreatedAt, before, after);
        Assert.InRange(capturedEntity.UpdatedAt, before, after);
    }

    [Fact]
    public async Task Handle_CaseInsensitiveUrgencyMatching()
    {
        // Arrange - Test with "EXPRESS" (uppercase)
        var command = new CreatePublicQuoteRequestCommand
        {
            Data = new PublicQuoteRequestDto
            {
                CustomerName = "Test",
                CustomerEmail = "test@example.com",
                Material = "Oak",
                Dimensions = new DimensionsDto { Length = 2000, Width = 1000, Thickness = 18 },
                Quantity = 1,
                EdgeType = "None",
                Surface = "Raw",
                Urgency = "EXPRESS" // Uppercase
            }
        };

        _mockRepository
            .Setup(r => r.AddPublicQuoteRequestAsync(It.IsAny<PublicQuoteRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("1 business day", result.Value!.EstimatedReplyTime);
    }
}
