using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Cutting.Application.DTOs.QuoteRequest;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using SpaceOS.Modules.Cutting.Tests.Fixtures;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Api;

/// <summary>
/// Integration tests for PublicQuoteRequest endpoint (MSG-BACKEND-079 Phase 4).
/// POST /api/public/cutting/quote-request
/// </summary>
public sealed class PublicQuoteRequestEndpointTests : IClassFixture<CuttingWebApplicationFactory>
{
    private readonly CuttingWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PublicQuoteRequestEndpointTests(CuttingWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── 1. Happy path: Valid request → 201 Created ────────────────────────

    [Fact]
    public async Task CreatePublicQuoteRequest_ValidData_Returns201Created()
    {
        // Arrange
        var request = new PublicQuoteRequestDto
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

        // Act
        var response = await _client.PostAsJsonAsync("/api/public/cutting/quote-request", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<PublicQuoteResponseDto>();
        result.Should().NotBeNull();
        result!.QuoteId.Should().NotBeEmpty();
        result.Status.Should().Be("received");
        result.EstimatedReplyTime.Should().Be("2 business days");
        result.TrackingUrl.Should().MatchRegex(@"^/public/quote/[\da-f-]{36}/status$");

        // Verify Location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain(result.QuoteId.ToString());
    }

    // ── 2. Express urgency → 1 business day ───────────────────────────────

    [Fact]
    public async Task CreatePublicQuoteRequest_ExpressUrgency_Returns1BusinessDay()
    {
        // Arrange
        var request = new PublicQuoteRequestDto
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

        // Act
        var response = await _client.PostAsJsonAsync("/api/public/cutting/quote-request", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<PublicQuoteResponseDto>();
        result!.EstimatedReplyTime.Should().Be("1 business day");
    }

    // ── 3. Validation tests ───────────────────────────────────────────────
    // Phase 5: FluentValidation tests

    [Fact]
    public async Task CreatePublicQuoteRequest_InvalidEmail_Returns400()
    {
        var request = new PublicQuoteRequestDto
        {
            CustomerName = "Test User",
            CustomerEmail = "invalid-email",  // Invalid format
            Material = "MDF 18mm",
            Dimensions = new DimensionsDto { Length = 600, Width = 400, Thickness = 18 },
            Quantity = 1,
            EdgeType = "ABS",
            Surface = "Painted",
            Urgency = "standard"
        };

        var response = await _client.PostAsJsonAsync("/api/public/cutting/quote-request", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("email");
    }

    [Fact]
    public async Task CreatePublicQuoteRequest_InvalidDimensions_Returns400()
    {
        var request = new PublicQuoteRequestDto
        {
            CustomerName = "Test User",
            CustomerEmail = "test@example.com",
            Material = "MDF 18mm",
            Dimensions = new DimensionsDto { Length = 15000, Width = 400, Thickness = 18 },  // Length > 10000
            Quantity = 1,
            EdgeType = "ABS",
            Surface = "Painted",
            Urgency = "standard"
        };

        var response = await _client.PostAsJsonAsync("/api/public/cutting/quote-request", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Length");
    }

    [Fact]
    public async Task CreatePublicQuoteRequest_ZeroQuantity_Returns400()
    {
        var request = new PublicQuoteRequestDto
        {
            CustomerName = "Test User",
            CustomerEmail = "test@example.com",
            Material = "MDF 18mm",
            Dimensions = new DimensionsDto { Length = 600, Width = 400, Thickness = 18 },
            Quantity = 0,  // Invalid
            EdgeType = "ABS",
            Surface = "Painted",
            Urgency = "standard"
        };

        var response = await _client.PostAsJsonAsync("/api/public/cutting/quote-request", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Quantity");
    }

    [Fact]
    public async Task CreatePublicQuoteRequest_MissingRequiredFields_Returns400()
    {
        var request = new PublicQuoteRequestDto
        {
            CustomerName = "",  // Empty
            CustomerEmail = "test@example.com",
            Material = "MDF 18mm",
            Dimensions = new DimensionsDto { Length = 600, Width = 400, Thickness = 18 },
            Quantity = 1,
            EdgeType = "ABS",
            Surface = "Painted",
            Urgency = "standard"
        };

        var response = await _client.PostAsJsonAsync("/api/public/cutting/quote-request", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("CustomerName");
    }

    [Fact]
    public async Task CreatePublicQuoteRequest_InvalidUrgency_Returns400()
    {
        var request = new PublicQuoteRequestDto
        {
            CustomerName = "Test User",
            CustomerEmail = "test@example.com",
            Material = "MDF 18mm",
            Dimensions = new DimensionsDto { Length = 600, Width = 400, Thickness = 18 },
            Quantity = 1,
            EdgeType = "ABS",
            Surface = "Painted",
            Urgency = "super-urgent"  // Invalid value
        };

        var response = await _client.PostAsJsonAsync("/api/public/cutting/quote-request", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Urgency");
    }

    // ── 4. Database persistence verification ──────────────────────────────

    [Fact]
    public async Task CreatePublicQuoteRequest_ValidData_PersistsToDatabase()
    {
        // Arrange
        var request = new PublicQuoteRequestDto
        {
            CustomerName = "Database Test User",
            CustomerEmail = "dbtest@example.com",
            Material = "Plywood 12mm",
            Dimensions = new DimensionsDto { Length = 800, Width = 600, Thickness = 12 },
            Quantity = 3,
            EdgeType = "PVC",
            Surface = "Veneered",
            Urgency = "standard"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/public/cutting/quote-request", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PublicQuoteResponseDto>();

        // Assert - verify database persistence
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CuttingDbContext>();

        var persistedQuote = await dbContext.PublicQuoteRequests
            .FindAsync(result!.QuoteId);

        persistedQuote.Should().NotBeNull();
        persistedQuote!.CustomerName.Should().Be("Database Test User");
        persistedQuote.CustomerEmail.Should().Be("dbtest@example.com");
        persistedQuote.Material.Should().Be("Plywood 12mm");
        persistedQuote.LengthMm.Should().Be(800);
        persistedQuote.WidthMm.Should().Be(600);
        persistedQuote.ThicknessMm.Should().Be(12);
        persistedQuote.Quantity.Should().Be(3);
        persistedQuote.Status.Should().Be("received");
    }

    // ── 5. Concurrent requests → unique GUIDs ─────────────────────────────

    [Fact]
    public async Task CreatePublicQuoteRequest_ConcurrentRequests_GeneratesUniqueIds()
    {
        // Arrange
        var requests = Enumerable.Range(1, 5).Select(i => new PublicQuoteRequestDto
        {
            CustomerName = $"Concurrent User {i}",
            CustomerEmail = $"concurrent{i}@example.com",
            Material = "MDF 18mm",
            Dimensions = new DimensionsDto { Length = 600, Width = 400, Thickness = 18 },
            Quantity = 1,
            EdgeType = "ABS",
            Surface = "Painted",
            Urgency = "standard"
        }).ToList();

        // Act - send concurrent requests
        var tasks = requests.Select(req =>
            _client.PostAsJsonAsync("/api/public/cutting/quote-request", req));

        var responses = await Task.WhenAll(tasks);

        // Assert - all successful
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.Created));

        // Extract all quote IDs
        var quoteIds = new List<Guid>();
        foreach (var response in responses)
        {
            var result = await response.Content.ReadFromJsonAsync<PublicQuoteResponseDto>();
            quoteIds.Add(result!.QuoteId);
        }

        // Verify uniqueness
        quoteIds.Should().OnlyHaveUniqueItems();
        quoteIds.Should().HaveCount(5);
    }

    // ── 6. Response structure validation ──────────────────────────────────

    [Fact]
    public async Task CreatePublicQuoteRequest_ValidData_ReturnsCorrectResponseStructure()
    {
        // Arrange
        var request = new PublicQuoteRequestDto
        {
            CustomerName = "Response Structure Test",
            CustomerEmail = "structure@example.com",
            Material = "Chipboard 16mm",
            Dimensions = new DimensionsDto { Length = 900, Width = 700, Thickness = 16 },
            Quantity = 2,
            EdgeType = "Melamine",
            Surface = "Laminated",
            Urgency = "standard"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/public/cutting/quote-request", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<PublicQuoteResponseDto>();

        // Verify all required fields are present and non-empty
        result.Should().NotBeNull();
        result!.QuoteId.Should().NotBeEmpty();
        result.Status.Should().NotBeNullOrEmpty();
        result.EstimatedReplyTime.Should().NotBeNullOrEmpty();
        result.TrackingUrl.Should().NotBeNullOrEmpty();
    }

    // ── 7. Optional fields handling ───────────────────────────────────────

    [Fact]
    public async Task CreatePublicQuoteRequest_OptionalFieldsNull_Returns201()
    {
        // Arrange
        var request = new PublicQuoteRequestDto
        {
            CustomerName = "Minimal Fields User",
            CustomerEmail = "minimal@example.com",
            CustomerPhone = null,  // Optional
            CompanyName = null,    // Optional
            Material = "MDF 18mm",
            Dimensions = new DimensionsDto { Length = 600, Width = 400, Thickness = 18 },
            Quantity = 1,
            EdgeType = "ABS",
            Surface = "Painted",
            Urgency = "standard",
            Notes = null  // Optional
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/public/cutting/quote-request", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<PublicQuoteResponseDto>();
        result.Should().NotBeNull();
        result!.QuoteId.Should().NotBeEmpty();
    }

    // ── 8. Endpoint allows anonymous access ───────────────────────────────

    [Fact]
    public async Task CreatePublicQuoteRequest_NoAuthToken_Returns201()
    {
        // Arrange
        var request = new PublicQuoteRequestDto
        {
            CustomerName = "Anonymous User",
            CustomerEmail = "anon@example.com",
            Material = "MDF 18mm",
            Dimensions = new DimensionsDto { Length = 600, Width = 400, Thickness = 18 },
            Quantity = 1,
            EdgeType = "ABS",
            Surface = "Painted",
            Urgency = "standard"
        };

        // Act - no authentication headers
        var response = await _client.PostAsJsonAsync("/api/public/cutting/quote-request", request);

        // Assert - should succeed (AllowAnonymous endpoint)
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
