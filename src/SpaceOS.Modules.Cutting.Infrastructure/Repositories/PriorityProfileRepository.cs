using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;

namespace SpaceOS.Modules.Cutting.Infrastructure.Repositories;

public class PriorityProfileRepository : IPriorityProfileRepository
{
    private readonly CuttingDbContext _db;

    public PriorityProfileRepository(CuttingDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(PriorityProfile profile, CancellationToken ct = default)
        => await _db.PriorityProfiles.AddAsync(profile, ct).ConfigureAwait(false);

    public async Task<PriorityProfile?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.PriorityProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<PriorityProfile>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.PriorityProfiles.AsNoTracking()
            .Where(p => p.TenantId == tenantId || p.TenantId == null)
            .OrderByDescending(p => p.IsDefault)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<PriorityProfile?> GetDefaultAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.PriorityProfiles.AsNoTracking()
            .Where(p => (p.TenantId == tenantId || p.TenantId == null) && p.IsDefault)
            .OrderBy(p => p.TenantId == null) // tenant-specific first, then global
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<PriorityProfile>> GetGlobalPresetsAsync(CancellationToken ct = default)
        => await _db.PriorityProfiles.AsNoTracking()
            .Where(p => p.TenantId == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct).ConfigureAwait(false);
}
