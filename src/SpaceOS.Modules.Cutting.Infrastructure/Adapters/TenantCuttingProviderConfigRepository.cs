using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Application.Adapters;
using SpaceOS.Modules.Cutting.Domain.Adapters;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters;

internal sealed class TenantCuttingProviderConfigRepository : ITenantCuttingProviderConfigRepository
{
    private readonly CuttingDbContext _db;

    public TenantCuttingProviderConfigRepository(CuttingDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    public async Task<TenantCuttingProviderConfig?> GetByTenantAsync(Guid tenantId, CancellationToken ct) =>
        await _db.TenantCuttingProviderConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct)
            .ConfigureAwait(false);

    public async Task AddAsync(TenantCuttingProviderConfig config, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(config);
        _db.TenantCuttingProviderConfigs.Add(config);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(TenantCuttingProviderConfig config, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(config);
        _db.TenantCuttingProviderConfigs.Update(config);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
