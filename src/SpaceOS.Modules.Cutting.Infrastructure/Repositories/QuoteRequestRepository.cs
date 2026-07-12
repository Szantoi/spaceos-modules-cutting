using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;

namespace SpaceOS.Modules.Cutting.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for CuttingQuoteRequest aggregate.
/// </summary>
public sealed class QuoteRequestRepository : IQuoteRequestRepository
{
    private readonly CuttingDbContext _db;

    public QuoteRequestRepository(CuttingDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc />
    public async Task AddAsync(CuttingQuoteRequest quoteRequest, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(quoteRequest);
        await _db.QuoteRequests.AddAsync(quoteRequest, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CuttingQuoteRequest?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.QuoteRequests
            .FirstOrDefaultAsync(q => q.Id == id, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CuttingQuoteRequest?> GetByTrackingTokenAsync(string trackingToken, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingToken);

        return await _db.QuoteRequests
            .FirstOrDefaultAsync(q => q.TrackingToken == trackingToken, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<List<CuttingQuoteRequest>> GetByTenantAsync(Guid tenantId, QuoteStatus? status, CancellationToken ct)
    {
        var query = _db.QuoteRequests
            .Where(q => q.TenantId == tenantId);

        if (status.HasValue)
        {
            query = query.Where(q => q.Status == status.Value);
        }

        return await query
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(CuttingQuoteRequest quoteRequest, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(quoteRequest);
        _db.QuoteRequests.Update(quoteRequest);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid tenantId, string quoteNumber, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(quoteNumber);

        return await _db.QuoteRequests
            .AnyAsync(q => q.TenantId == tenantId && q.QuoteNumber == quoteNumber, ct)
            .ConfigureAwait(false);
    }
}
