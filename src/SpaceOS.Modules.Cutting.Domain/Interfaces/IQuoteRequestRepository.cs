using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Domain.Interfaces;

/// <summary>
/// Repository interface for CuttingQuoteRequest aggregate.
/// </summary>
public interface IQuoteRequestRepository
{
    /// <summary>
    /// Adds a new quote request.
    /// </summary>
    Task AddAsync(CuttingQuoteRequest quoteRequest, CancellationToken ct);

    /// <summary>
    /// Gets a quote request by ID.
    /// </summary>
    Task<CuttingQuoteRequest?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Gets a quote request by tracking token.
    /// </summary>
    Task<CuttingQuoteRequest?> GetByTrackingTokenAsync(string trackingToken, CancellationToken ct);

    /// <summary>
    /// Gets all quote requests for a tenant, optionally filtered by status.
    /// </summary>
    Task<List<CuttingQuoteRequest>> GetByTenantAsync(Guid tenantId, QuoteStatus? status, CancellationToken ct);

    /// <summary>
    /// Updates an existing quote request.
    /// </summary>
    Task UpdateAsync(CuttingQuoteRequest quoteRequest, CancellationToken ct);

    /// <summary>
    /// Checks if a quote number already exists for a tenant.
    /// </summary>
    Task<bool> ExistsAsync(Guid tenantId, string quoteNumber, CancellationToken ct);
}
