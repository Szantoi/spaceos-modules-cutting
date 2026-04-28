using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Application.Adapters;
using SpaceOS.Modules.Cutting.Domain.Adapters;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters;

internal sealed class AdapterHealthRecordRepository : IAdapterHealthRecordRepository
{
    private readonly CuttingDbContext _db;

    public AdapterHealthRecordRepository(CuttingDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    public async Task<AdapterHealthRecord?> GetAsync(Guid tenantId, string adapterName, CancellationToken ct) =>
        await _db.AdapterHealthRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.AdapterName == adapterName, ct)
            .ConfigureAwait(false);

    public async Task AddAsync(AdapterHealthRecord record, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(record);
        _db.AdapterHealthRecords.Add(record);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(AdapterHealthRecord record, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(record);
        _db.AdapterHealthRecords.Update(record);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
