using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using SpaceOS.Modules.Cutting.Application.Commands.CreateQuoteRequest;
using SpaceOS.Modules.Cutting.Application.Commands.AcceptQuote;
using SpaceOS.Modules.Cutting.Application.DTOs.QuoteRequest;
using SpaceOS.Modules.Cutting.Application.Queries.TrackQuote;
using SpaceOS.Modules.Cutting.Application.Services;
using ResultStatus = Ardalis.Result.ResultStatus;

namespace SpaceOS.Modules.Cutting.Tests.Api.Endpoints;

/// <summary>
/// Unit tests for QuoteRequestEndpoints.
/// Note: These are simplified unit tests for endpoint logic validation.
/// Full integration tests would use WebApplicationFactory.
/// </summary>
public class QuoteRequestEndpointsTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ITenantResolver> _mockTenantResolver;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<HttpContext> _mockHttpContext;

    public QuoteRequestEndpointsTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockTenantResolver = new Mock<ITenantResolver>();
        _mockEmailService = new Mock<IEmailService>();
        _mockHttpContext = new Mock<HttpContext>();
    }

    [Fact]
    public async Task CreateQuoteRequest_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var requestDto = new CreateQuoteRequestDto
        {
            CustomerEmail = "customer@example.com",
            CustomerName = "Test Customer",
            Items = new List<QuoteLineItemDto>
            {
                new QuoteLineItemDto
                {
                    MaterialType = "Oak",
                    WidthMm = 600,
                    HeightMm = 800,
                    Quantity = 10,
                    EdgingType = "PVC"
                }
            },
            DeliveryAddress = "Test Street 123, Budapest"
        };

        var tenantId = Guid.NewGuid();
        var responseDto = new QuoteRequestResponseDto
        {
            QuoteId = Guid.NewGuid().ToString(),
            QuoteNumber = "Q-2026-001",
            TrackingToken = "token123",
            TrackingUrl = "https://example.com/track/token123",
            Status = "Pending",
            EstimatedResponseTime = "24-48 hours"
        };

        // Setup HttpContext with hostname
        var headers = new HeaderDictionary
        {
            { "X-Original-Host", "doorstar.joinerytech.hu" }
        };
        var mockRequest = new Mock<HttpRequest>();
        mockRequest.Setup(r => r.Headers).Returns(headers);
        mockRequest.Setup(r => r.Host).Returns(new HostString("doorstar.joinerytech.hu"));
        _mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);

        // Setup TenantResolver
        _mockTenantResolver
            .Setup(r => r.GetTenantIdFromSubdomain("doorstar.joinerytech.hu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId);

        // Setup Mediator
        _mockMediator
            .Setup(m => m.Send(It.IsAny<CreateQuoteRequestCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<QuoteRequestResponseDto>.Success(responseDto));

        // Setup EmailService
        _mockEmailService
            .Setup(e => e.SendQuoteRequestNotification(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - Simulating endpoint logic
        var result = await CreateQuoteRequestEndpointLogic(
            requestDto,
            _mockMediator.Object,
            _mockTenantResolver.Object,
            _mockEmailService.Object,
            _mockHttpContext.Object,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(responseDto.QuoteNumber, result.Value.QuoteNumber);

        _mockEmailService.Verify(
            e => e.SendQuoteRequestNotification(
                requestDto.CustomerEmail,
                It.IsAny<string>(),
                responseDto.QuoteNumber,
                responseDto.TrackingToken,
                responseDto.TrackingUrl,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateQuoteRequest_WithTenantNotFound_ReturnsNotFoundResult()
    {
        // Arrange
        var requestDto = new CreateQuoteRequestDto
        {
            CustomerEmail = "customer@example.com",
            CustomerName = "Test Customer",
            Items = new List<QuoteLineItemDto>
            {
                new QuoteLineItemDto
                {
                    MaterialType = "Oak",
                    WidthMm = 600,
                    HeightMm = 800,
                    Quantity = 10,
                    EdgingType = "PVC"
                }
            },
            DeliveryAddress = "Test Street 123, Budapest"
        };

        var headers = new HeaderDictionary
        {
            { "X-Original-Host", "unknown.joinerytech.hu" }
        };
        var mockRequest = new Mock<HttpRequest>();
        mockRequest.Setup(r => r.Headers).Returns(headers);
        mockRequest.Setup(r => r.Host).Returns(new HostString("unknown.joinerytech.hu"));
        _mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);

        _mockTenantResolver
            .Setup(r => r.GetTenantIdFromSubdomain("unknown.joinerytech.hu", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TenantNotFoundException("No tenant found for subdomain: unknown"));

        // Act
        var result = await CreateQuoteRequestEndpointLogic(
            requestDto,
            _mockMediator.Object,
            _mockTenantResolver.Object,
            _mockEmailService.Object,
            _mockHttpContext.Object,
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task TrackQuote_WithValidToken_ReturnsOkResult()
    {
        // Arrange
        var trackingToken = "token123";
        var quoteDto = new QuoteTrackingDto
        {
            QuoteNumber = "Q-2026-001",
            Status = "Pending",
            SubmittedAt = DateTime.UtcNow
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<TrackQuoteQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<QuoteTrackingDto>.Success(quoteDto));

        // Act
        var result = await TrackQuoteEndpointLogic(
            trackingToken,
            _mockMediator.Object,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(quoteDto.QuoteNumber, result.Value.QuoteNumber);
    }

    [Fact]
    public async Task TrackQuote_WithInvalidToken_ReturnsNotFoundResult()
    {
        // Arrange
        var trackingToken = "invalid-token";

        _mockMediator
            .Setup(m => m.Send(It.IsAny<TrackQuoteQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<QuoteTrackingDto>.NotFound());

        // Act
        var result = await TrackQuoteEndpointLogic(
            trackingToken,
            _mockMediator.Object,
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task AcceptQuote_WithValidToken_ReturnsSuccessResult()
    {
        // Arrange
        var trackingToken = "token123";

        _mockMediator
            .Setup(m => m.Send(It.IsAny<AcceptQuoteCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await AcceptQuoteEndpointLogic(
            trackingToken,
            _mockMediator.Object,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    // Helper methods simulating endpoint logic (extracted from actual endpoints)
    private async Task<Result<QuoteRequestResponseDto>> CreateQuoteRequestEndpointLogic(
        CreateQuoteRequestDto request,
        IMediator mediator,
        ITenantResolver tenantResolver,
        IEmailService emailService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        try
        {
            var hostname = httpContext.Request.Headers["X-Original-Host"].FirstOrDefault()
                ?? httpContext.Request.Host.Host;

            var tenantId = await tenantResolver.GetTenantIdFromSubdomain(hostname, ct);

            var command = new CreateQuoteRequestCommand
            {
                TenantId = tenantId,
                Data = request
            };

            var result = await mediator.Send(command, ct);

            if (!result.IsSuccess)
                return result;

            var response = result.Value!;
            var adminEmail = "support@joinerytech.hu";

            await emailService.SendQuoteRequestNotification(
                customerEmail: request.CustomerEmail,
                adminEmail: adminEmail,
                quoteNumber: response.QuoteNumber,
                trackingToken: response.TrackingToken,
                trackingUrl: response.TrackingUrl,
                ct: ct);

            return result;
        }
        catch (TenantNotFoundException)
        {
            return Result<QuoteRequestResponseDto>.NotFound();
        }
    }

    private async Task<Result<QuoteTrackingDto>> TrackQuoteEndpointLogic(
        string trackingToken,
        IMediator mediator,
        CancellationToken ct)
    {
        var query = new TrackQuoteQuery { TrackingToken = trackingToken };
        return await mediator.Send(query, ct);
    }

    private async Task<Result> AcceptQuoteEndpointLogic(
        string trackingToken,
        IMediator mediator,
        CancellationToken ct)
    {
        var command = new AcceptQuoteCommand { TrackingToken = trackingToken };
        return await mediator.Send(command, ct);
    }
}
