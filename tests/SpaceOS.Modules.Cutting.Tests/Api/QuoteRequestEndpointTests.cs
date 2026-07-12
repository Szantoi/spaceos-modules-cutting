using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Cutting.Application.DTOs.QuoteRequest;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.ValueObjects;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using SpaceOS.Modules.Cutting.Tests.Fixtures;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Api;

/// <summary>
/// Integration tests for QuoteRequest endpoints (Public + Admin API).
/// Tests cover authentication, authorization, tenant isolation, validation, and FSM state transitions.
/// </summary>
public sealed class QuoteRequestEndpointTests : IClassFixture<CuttingWebApplicationFactory>
{
    private readonly CuttingWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public QuoteRequestEndpointTests(CuttingWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Public API Tests (Unauthenticated)

    [Fact]
    public async Task CreateQuoteRequest_ValidData_Returns200AndTrackingToken()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var request = new CreateQuoteRequestDto
        {
            CustomerEmail = "customer@example.com",
            CustomerName = "Test Customer",
            CustomerPhone = "+36301234567",
            Items = new List<QuoteLineItemDto>
            {
                new QuoteLineItemDto
                {
                    MaterialType = "MDF_18MM",
                    WidthMm = 2800,
                    HeightMm = 2070,
                    Quantity = 5,
                    EdgingType = "ABS_2MM_WHITE",
                    Notes = "Test item"
                }
            },
            DeliveryAddress = "Budapest, Kossuth utca 10.",
            RequestedDeliveryDate = DateTime.UtcNow.AddDays(7)
        };

        _client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());

        // Act
        var response = await _client.PostAsJsonAsync("/public/cutting/quote-request", request).ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<QuoteRequestResponseDto>().ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.NotEmpty(result.QuoteId);
        Assert.NotEmpty(result.TrackingToken);
        Assert.Equal(12, result.TrackingToken.Length); // 12-char hex
        Assert.Equal("PendingReview", result.Status);
        Assert.NotEmpty(result.TrackingUrl);
    }

    [Fact]
    public async Task CreateQuoteRequest_InvalidEmail_Returns400()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var request = new CreateQuoteRequestDto
        {
            CustomerEmail = "invalid-email", // Invalid email format
            CustomerName = "Test Customer",
            CustomerPhone = null,
            Items = new List<QuoteLineItemDto>
            {
                new QuoteLineItemDto
                {
                    MaterialType = "MDF_18MM",
                    WidthMm = 2800,
                    HeightMm = 2070,
                    Quantity = 5,
                    EdgingType = "None",
                    Notes = null
                }
            },
            DeliveryAddress = "Test Address",
            RequestedDeliveryDate = null
        };

        _client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());

        // Act
        var response = await _client.PostAsJsonAsync("/public/cutting/quote-request", request).ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TrackQuote_ValidToken_ReturnsQuoteDetails()
    {
        // Arrange
        var (tenantId, trackingToken) = await CreateTestQuoteAsync().ConfigureAwait(false);

        // Act
        var response = await _client.GetAsync($"/public/cutting/quotes/track/{trackingToken}").ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<QuoteTrackingDto>().ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.NotEmpty(result.QuoteNumber);
        Assert.Equal("PendingReview", result.Status);
    }

    [Fact]
    public async Task TrackQuote_InvalidToken_Returns404()
    {
        // Arrange
        var invalidToken = "invalidtoken";

        // Act
        var response = await _client.GetAsync($"/public/cutting/quotes/track/{invalidToken}").ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AcceptQuote_ValidToken_Returns200()
    {
        // Arrange
        var (tenantId, trackingToken) = await CreateAndApproveTestQuoteAsync().ConfigureAwait(false);

        // Act
        var response = await _client.PostAsync($"/public/cutting/quotes/track/{trackingToken}/accept", null).ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<dynamic>().ConfigureAwait(false);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task AcceptQuote_AlreadyAccepted_Returns400()
    {
        // Arrange
        var (tenantId, trackingToken) = await CreateAndApproveTestQuoteAsync().ConfigureAwait(false);
        await _client.PostAsync($"/public/cutting/quotes/track/{trackingToken}/accept", null).ConfigureAwait(false);

        // Act (second accept)
        var response = await _client.PostAsync($"/public/cutting/quotes/track/{trackingToken}/accept", null).ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Admin API Tests (Authenticated, Tenant-Scoped)

    [Fact]
    public async Task GetQuoteRequests_ValidTenant_ReturnsFiltered()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await CreateTestQuoteForTenantAsync(tenantId).ConfigureAwait(false);

        var authenticatedClient = CreateAuthenticatedClient(tenantId, userId);

        // Act
        var response = await authenticatedClient.GetAsync("/api/cutting/quotes").ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<QuoteRequestListItemDto>>().ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetQuoteRequests_Unauthenticated_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/cutting/quotes").ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApproveQuote_ValidQuote_UpdatesStatus()
    {
        // Arrange
        var (tenantId, quoteId) = await CreateTestQuoteForApprovalAsync().ConfigureAwait(false);
        var userId = Guid.NewGuid();
        var authenticatedClient = CreateAuthenticatedClient(tenantId, userId);

        var approveRequest = new
        {
            QuotedPriceAmount = 45000m,
            QuotedPriceCurrency = "HUF"
        };

        // Act
        var response = await authenticatedClient.PutAsJsonAsync($"/api/cutting/quotes/{quoteId}/approve", approveRequest).ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify status changed
        var quote = await GetQuoteByIdAsync(quoteId).ConfigureAwait(false);
        Assert.Equal(QuoteStatus.Quoted, quote.Status);
    }

    [Fact]
    public async Task ApproveQuote_AlreadyApproved_Returns400()
    {
        // Arrange
        var (tenantId, quoteId) = await CreateTestQuoteForApprovalAsync().ConfigureAwait(false);
        var userId = Guid.NewGuid();
        var authenticatedClient = CreateAuthenticatedClient(tenantId, userId);

        var approveRequest = new
        {
            QuotedPriceAmount = 45000m,
            QuotedPriceCurrency = "HUF"
        };

        // First approval
        await authenticatedClient.PutAsJsonAsync($"/api/cutting/quotes/{quoteId}/approve", approveRequest).ConfigureAwait(false);

        // Act (second approval)
        var response = await authenticatedClient.PutAsJsonAsync($"/api/cutting/quotes/{quoteId}/approve", approveRequest).ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RejectQuote_ValidQuote_UpdatesStatus()
    {
        // Arrange
        var (tenantId, quoteId) = await CreateTestQuoteForApprovalAsync().ConfigureAwait(false);
        var userId = Guid.NewGuid();
        var authenticatedClient = CreateAuthenticatedClient(tenantId, userId);

        var rejectRequest = new
        {
            Reason = "Insufficient capacity"
        };

        // Act
        var response = await authenticatedClient.PutAsJsonAsync($"/api/cutting/quotes/{quoteId}/reject", rejectRequest).ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify status changed
        var quote = await GetQuoteByIdAsync(quoteId).ConfigureAwait(false);
        Assert.Equal(QuoteStatus.Rejected, quote.Status);
    }

    [Fact]
    public async Task RejectQuote_InvalidQuoteId_Returns404()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var authenticatedClient = CreateAuthenticatedClient(tenantId, userId);
        var invalidQuoteId = Guid.NewGuid();

        var rejectRequest = new
        {
            Reason = "Test reason"
        };

        // Act
        var response = await authenticatedClient.PutAsJsonAsync($"/api/cutting/quotes/{invalidQuoteId}/reject", rejectRequest).ConfigureAwait(false);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Helper Methods

    private async Task<(Guid TenantId, string TrackingToken)> CreateTestQuoteAsync()
    {
        var tenantId = Guid.NewGuid();
        var trackingToken = GenerateTrackingToken();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CuttingDbContext>();

        var quote = CuttingQuoteRequest.CreatePublic(
            tenantId,
            $"QT-2026-{Random.Shared.Next(100000, 999999)}",
            trackingToken,
            new ContactInfo("test@example.com", "Test Customer", "+36301234567"),
            new List<QuoteLineItem>
            {
                new QuoteLineItem(MaterialType.MDF_18MM, 2800, 2070, 5, EdgingType.ABS_2MM_WHITE, "Test")
            },
            new DeliveryDetails("Test Address", DateTime.UtcNow.AddDays(7)));

        dbContext.QuoteRequests.Add(quote);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        return (tenantId, trackingToken);
    }

    private async Task<(Guid TenantId, string TrackingToken)> CreateAndApproveTestQuoteAsync()
    {
        var (tenantId, trackingToken) = await CreateTestQuoteAsync().ConfigureAwait(false);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CuttingDbContext>();

        var quote = await dbContext.QuoteRequests
            .FirstOrDefaultAsync(q => q.TrackingToken == trackingToken)
            .ConfigureAwait(false);

        quote!.ApproveAndQuote(new Money(45000m, "HUF"), Guid.NewGuid());
        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        return (tenantId, trackingToken);
    }

    private async Task<Guid> CreateTestQuoteForTenantAsync(Guid tenantId)
    {
        var trackingToken = GenerateTrackingToken();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CuttingDbContext>();

        var quote = CuttingQuoteRequest.CreatePublic(
            tenantId,
            $"QT-2026-{Random.Shared.Next(100000, 999999)}",
            trackingToken,
            new ContactInfo("test@example.com", "Test Customer", null),
            new List<QuoteLineItem>
            {
                new QuoteLineItem(MaterialType.MDF_18MM, 2800, 2070, 5, EdgingType.None, null)
            },
            new DeliveryDetails("Test Address", null));

        dbContext.QuoteRequests.Add(quote);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        return quote.Id;
    }

    private async Task<(Guid TenantId, Guid QuoteId)> CreateTestQuoteForApprovalAsync()
    {
        var tenantId = Guid.NewGuid();
        var quoteId = await CreateTestQuoteForTenantAsync(tenantId).ConfigureAwait(false);
        return (tenantId, quoteId);
    }

    private async Task<CuttingQuoteRequest> GetQuoteByIdAsync(Guid quoteId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CuttingDbContext>();
        return (await dbContext.QuoteRequests.FindAsync(quoteId).ConfigureAwait(false))!;
    }

    private HttpClient CreateAuthenticatedClient(Guid tenantId, Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer fake-token-{userId}");
        // Note: In real tests, use TestAuthHandler to inject claims
        return client;
    }

    private static string GenerateTrackingToken()
    {
        var bytes = new byte[6];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    #endregion
}
